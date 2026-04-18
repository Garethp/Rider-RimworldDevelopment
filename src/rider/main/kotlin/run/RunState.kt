package RimworldDev.Rider.run

import RimworldDev.Rider.helpers.ScopeHelper
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
import kotlinx.coroutines.withContext
import java.io.File
import java.io.FileOutputStream
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
        setupDoorstop()
        QuickStartUtils.setup(modListPath, saveFilePath)

        // On macOS/Linux we must bypass rimworldState.execute() for two reasons:
        //  1. CommandLineState.execute() has implicit EDT dependencies (console view creation)
        //     that fail silently when called from this suspend function's coroutine context.
        //  2. On macOS, the 'open' command launches apps through launchd, which is SIP-protected
        //     and strips DYLD_INSERT_LIBRARIES — preventing Doorstop injection entirely.
        // ProcessBuilder with run.sh avoids both: it fork/exec's directly, preserving DYLD_* vars,
        // and has no IntelliJ threading requirements. Windows uses DLL hijacking (winhttp.dll)
        // instead of DYLD injection, so rimworldState.execute() works fine there.
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