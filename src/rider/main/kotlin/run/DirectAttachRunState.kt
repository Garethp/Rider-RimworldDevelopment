package RimworldDev.Rider.run

import com.intellij.execution.ExecutionResult
import com.intellij.execution.configurations.GeneralCommandLine
import com.intellij.execution.process.ProcessListener
import com.intellij.execution.process.ProcessEvent
import com.intellij.execution.process.ProcessInfo
import com.intellij.execution.process.ProcessTerminatedListener
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.ui.ConsoleView
import com.intellij.util.system.OS
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.model.debuggerHelper.PlatformArchitecture
import com.jetbrains.rider.run.dotNetCore.DotNetCoreAttachProfileState
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.withContext
import java.io.File

/**
 * Debugging for RimWorld 1.7+ means we no longer need Doorstop, we can just attach our debugger directly.
 */
class DirectAttachRunState private constructor(
    private val gameProcess: Process,
    private val saveFilePath: String,
    executionEnvironment: ExecutionEnvironment,
) : DotNetCoreAttachProfileState(
    ProcessInfo(gameProcess.pid().toInt(), gameProcess.info().commandLine().orElse(""), "RimWorld", ""),
    executionEnvironment,
    PlatformArchitecture.X64,
    "RimWorld"
) {
    override suspend fun execute(
        workerConsole: ConsoleView,
        workerProcessHandler: DebuggerWorkerProcessHandler,
        lifetime: Lifetime
    ): ExecutionResult {
        val result = try {
            super.execute(workerConsole, workerProcessHandler, lifetime)
        } catch (e: Throwable) {
            // e.g. the attach failing because the game already exited
            cleanUp(gameProcess, saveFilePath)
            throw e
        }

        ProcessTerminatedListener.attach(workerProcessHandler.debuggerWorkerRealHandler)
        workerProcessHandler.debuggerWorkerRealHandler.addProcessListener(object : ProcessListener {
            override fun processTerminated(event: ProcessEvent) {
                event.processHandler.removeProcessListener(this)
                cleanUp(gameProcess, saveFilePath)
            }
        })

        return result
    }

    companion object {
        suspend fun launch(
            commandLine: GeneralCommandLine,
            modListPath: String,
            saveFilePath: String,
            executionEnvironment: ExecutionEnvironment
        ): DirectAttachRunState {
            QuickStartUtils.setup(modListPath, saveFilePath)

            val gameProcess = withContext(Dispatchers.IO) {
                try {
                    // Nothing reads the game's output, so send it to a file like DoorstopRunState does. Left as
                    // a pipe, the game blocks writing its log once the pipe buffer fills.
                    val logFile = File(System.getProperty("java.io.tmpdir"), "rimworld-debug.log")
                    commandLine.toProcessBuilder()
                        .redirectErrorStream(true)
                        .redirectOutput(logFile)
                        .start()
                } catch (e: Exception) {
                    QuickStartUtils.tearDown(saveFilePath)
                    throw e
                }
            }

            // The game is running and the mod list is swapped from here on, but the cleanup listener isn't added until
            // execute(), so anything that stops us getting there (cancelling, a failed wait) has to clean up itself
            try {
                waitForDotNetRuntime(gameProcess)
            } catch (e: Throwable) {
                cleanUp(gameProcess, saveFilePath)
                throw e
            }

            return DirectAttachRunState(gameProcess, saveFilePath, executionEnvironment)
        }

        private fun cleanUp(gameProcess: Process, saveFilePath: String) {
            gameProcess.destroy()
            QuickStartUtils.tearDown(saveFilePath)
        }

        /**
         * Wait until we've actually loaded our Runtime
         */
        private suspend fun waitForDotNetRuntime(process: Process) = withContext(Dispatchers.IO) {
            val deadline = System.currentTimeMillis() + 30_000L
            while (System.currentTimeMillis() < deadline) {
                if (!process.isAlive) return@withContext
                if (hasLoadedRuntime(process.pid())) return@withContext
                delay(250)
            }
        }

        private fun hasLoadedRuntime(pid: Long): Boolean = try {
            when (OS.CURRENT) {
                OS.Windows -> runQuietly("tasklist", "/FI", "PID eq $pid", "/M", "coreclr.dll").contains("coreclr.dll", true)
                OS.Linux -> File("/proc/$pid/maps").readText().contains("libcoreclr.so")
                OS.macOS -> runQuietly("lsof", "-p", "$pid", "-Fn").contains("libcoreclr.dylib")
                else -> true
            }
        } catch (_: Exception) {
            false
        }

        private fun runQuietly(vararg command: String): String {
            val process = ProcessBuilder(*command).redirectErrorStream(true).start()
            return process.inputStream.bufferedReader().use { it.readText() }.also { process.waitFor() }
        }
    }
}
