package RimworldDev.Rider.run

import RimworldDev.Rider.helpers.ScopeHelper
import com.intellij.execution.ExecutionException
import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.process.*
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.util.system.OS
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachProfileState
import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import com.jetbrains.rider.run.getProcess
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.withContext
import java.io.File
import java.io.FileOutputStream
import java.net.BindException
import java.net.InetSocketAddress
import java.net.ServerSocket
import java.nio.file.Files
import kotlin.io.path.Path

class RunState(
    private val rimworldLocation: String,
    private val saveFilePath: String,
    private val modListPath: String,
    private val rimworldState: RunProfileState,
    remoteConfiguration: RemoteConfiguration,
    executionEnvironment: ExecutionEnvironment,
    targetName: String
) :
    UnityAttachProfileState(remoteConfiguration, executionEnvironment, targetName), RunProfileState {
    private val resources = mapOf(
        OS.Windows to listOf(
            ".doorstop_version",
            "doorstop_config.ini",
            "winhttp.dll",

            "Doorstop/0Harmony.dll",
            "Doorstop/dnlib.dll",
            "Doorstop/Doorstop.dll",
            "Doorstop/Doorstop.pdb",
            "Doorstop/HotReload.dll",
            "Doorstop/Mono.Cecil.dll",
            "Doorstop/Mono.CompilerServices.SymbolWriter.dll",
            "Doorstop/pdb2mdb.exe",
        ),
        OS.Linux to listOf(
            "run.sh",
            ".doorstop_version",

            "Doorstop/0Harmony.dll",
            "Doorstop/dnlib.dll",
            "Doorstop/Doorstop.dll",
            "Doorstop/Doorstop.pdb",
            "Doorstop/HotReload.dll",
            "Doorstop/libdoorstop.so",
            "Doorstop/Mono.Cecil.dll",
            "Doorstop/Mono.CompilerServices.SymbolWriter.dll",
            "Doorstop/pdb2mdb.exe",
        ),
        // macOS resource list was previously incomplete. Added:
        //   - "run.sh"              — the launch script that sets DYLD_INSERT_LIBRARIES and resolves the real binary inside the .app bundle
        //   - "Doorstop/dnlib.dll" — required by Doorstop at load time; omission caused silent injection failure
        //   - "Doorstop/HotReload.dll" — likewise required but was missing
        //   - "Doorstop/libdoorstop.dylib" — the native DYLD injection library; without this Doorstop cannot intercept Mono at all
        // Also fixed: the config file was previously listed as ".doorstop_config.ini" (with a leading dot).
        // getResourceAsStream() returned null for that name (the actual resource has no leading dot), and the
        // copyResource() function silently returned on null — so the config file was never copied.
        OS.macOS to listOf(
            "run.sh",
            ".doorstop_version",
            "doorstop_config.ini",

            "Doorstop/0Harmony.dll",
            "Doorstop/dnlib.dll",
            "Doorstop/Doorstop.dll",
            "Doorstop/Doorstop.pdb",
            "Doorstop/HotReload.dll",
            "Doorstop/libdoorstop.dylib",
            "Doorstop/Mono.Cecil.dll",
            "Doorstop/Mono.CompilerServices.SymbolWriter.dll",
            "Doorstop/pdb2mdb.exe",
        )
    )
    
    override suspend fun execute(
        executor: Executor,
        runner: ProgramRunner<*>,
        workerProcessHandler: DebuggerWorkerProcessHandler,
        lifetime: Lifetime
    ): ExecutionResult {
        // Order matters: Doorstop files and the quick-start mod/save setup must both be in place
        // before the game process starts. Previously, setup() was called inside rimworldState's
        // startProcess(), but that path is bypassed on macOS/Linux (see ProcessBuilder block below),
        // so it is called explicitly here instead.
        setupDoorstop()
        QuickStartUtils.setup(modListPath, saveFilePath)

        // On macOS/Linux, launch the game directly via ProcessBuilder rather than going through
        // rimworldState.execute(). The IntelliJ process framework can fail silently in the
        // Rider sandbox's coroutine context. ProcessBuilder is reliable and redirects
        // Doorstop's verbose stdout (MEMORY MAP + BIND_OPCODE dump) directly to a temp file —
        // this avoids the 64KB pipe buffer deadlock without needing a drain thread, and
        // preserves the output for diagnostics.
        val gameProcess: Process? = if (OS.CURRENT == OS.macOS || OS.CURRENT == OS.Linux) {
            val bashScriptPath = "${Path(rimworldLocation).parent}/run.sh"
            withContext(Dispatchers.IO) {
                val logFile = File(System.getProperty("java.io.tmpdir"), "rimworld-doorstop.log")
                ProcessBuilder("/bin/sh", bashScriptPath, rimworldLocation)
                    .redirectErrorStream(true)
                    .redirectOutput(logFile)
                    .start()
            }
        } else {
            // Windows: rimworldState handles env vars and process lifecycle normally.
            rimworldState.execute(executor, runner)?.processHandler?.getProcess()
        }

        // Poll until the Mono debug server is reachable. With debug_suspend=true in run.sh,
        // the port stays open indefinitely once the game starts — no race condition.
        if (!waitForMonoDebugServer()) {
            throw ExecutionException(
                "Timed out waiting for Mono debug server on port 56000. " +
                "The game process may have failed to start, or Doorstop may not have injected. " +
                "Check that run.sh is executable and that libdoorstop.dylib is present in the game directory."
            )
        }

        val result = super.execute(executor, runner, workerProcessHandler)
        ProcessTerminatedListener.attach(workerProcessHandler.debuggerWorkerRealHandler)
        workerProcessHandler.debuggerWorkerRealHandler.addProcessListener(object : ProcessAdapter() {
            override fun processTerminated(event: ProcessEvent) {
                event.processHandler.removeProcessListener(this)
                if (OS.CURRENT == OS.Linux) {
                    gameProcess?.destroyForcibly()
                } else {
                    gameProcess?.destroy()
                }
                QuickStartUtils.tearDown(saveFilePath)
                removeDoorstep()
            }
        })

        return result
    }

    private suspend fun waitForMonoDebugServer(port: Int = 56000, timeoutMs: Long = 60_000): Boolean {
        val deadline = System.currentTimeMillis() + timeoutMs
        while (System.currentTimeMillis() < deadline) {
            val isListening = withContext(Dispatchers.IO) {
                try {
                    // Bind to 127.0.0.1 explicitly — the same address Mono uses.
                    // On macOS, 0.0.0.0 and 127.0.0.1 are distinct bind targets, so
                    // ServerSocket(port) (which binds 0.0.0.0) would NOT throw BindException
                    // even when Mono is already listening on 127.0.0.1:port.
                    ServerSocket().use { it.bind(InetSocketAddress("127.0.0.1", port)) }
                    false // bound successfully — nothing listening yet
                } catch (_: BindException) {
                    true  // address in use — Mono debug server is up
                } catch (_: Exception) {
                    false
                }
            }
            if (isListening) return true
            delay(500)
        }
        return false
    }

    private fun setupDoorstop() {
        val currentResources = resources[OS.CURRENT] ?: return

        val rimworldDir = Path(rimworldLocation).parent.toFile()

        val copyResource = fun(basePath: String, name: String) {
            val file = File(rimworldDir, name)
            if (!file.parentFile.exists()) {
                Files.createDirectory(file.parentFile.toPath())
            }

            file.createNewFile()

            val resourceStream = this.javaClass.getResourceAsStream("$basePath/$name") ?: return

            val fileWriteStream = FileOutputStream(file)
            fileWriteStream.write(resourceStream.readAllBytes())
            fileWriteStream.close()
        }

        currentResources.forEach {
            copyResource("/UnityDoorstop/${OS.CURRENT}/", it)
        }

        val harmonyDll = ScopeHelper.findInstalledHarmonyDll()
        if (harmonyDll != null) {
            File(harmonyDll).copyTo(File("${rimworldDir}/Doorstop/0Harmony.dll"), true)
        }
    }

    private fun removeDoorstep() {
        val currentResources = resources[OS.CURRENT] ?: return
        Thread.sleep(50)

        val rimworldDir = Path(rimworldLocation).parent.toFile()

        val removeResource = fun(name: String) {
            val file = File(rimworldDir, name)
            file.delete()
        }

        currentResources.forEach {
            removeResource(it)
        }

        removeResource("Doorstop/")
    }
}