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
        setupDoorstop()

        val result = super.execute(executor, runner, workerProcessHandler)
        ProcessTerminatedListener.attach(workerProcessHandler.debuggerWorkerRealHandler)

        val rimworldResult = rimworldState.execute(executor, runner)
        workerProcessHandler.debuggerWorkerRealHandler.addProcessListener(createProcessListener(rimworldResult?.processHandler))

        return result
    }

    private fun createProcessListener(siblingProcessHandler: ProcessHandler?): ProcessListener {
        return object : ProcessAdapter() {
            override fun processTerminated(event: ProcessEvent) {
                val processHandler = event.processHandler
                processHandler.removeProcessListener(this)

                if (OS.CURRENT == OS.Linux) {
                    siblingProcessHandler?.getProcess()?.destroyForcibly()
                } else {
                    siblingProcessHandler?.getProcess()?.destroy()
                }

                QuickStartUtils.tearDown(saveFilePath)
                removeDoorstep()
            }
        }
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