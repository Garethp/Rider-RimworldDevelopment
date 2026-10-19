package RimworldDev.Rider.run

import RimworldDev.Rider.helpers.ScopeHelper
import com.intellij.execution.Executor
import com.intellij.execution.configuration.EnvironmentVariablesData
import com.intellij.execution.configurations.*
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.execution.process.*
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.util.system.OS
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.run.configurations.AsyncRunConfiguration
import org.jetbrains.concurrency.Promise
import com.jetbrains.rider.plugins.unity.run.configurations.unityExe.UnityExeConfiguration
import com.jetbrains.rider.run.RiderRunBundle
import java.io.File

class RunConfiguration(project: Project, factory: ConfigurationFactory, name: String) :
    RunConfigurationBase<ConfigurationOptions>(project, factory, name),
    LocatableConfiguration,
    RunProfileWithCompileBeforeLaunchOption,
    RunConfigurationWithSuppressedDefaultDebugAction,
    AsyncRunConfiguration,
    IRiderDebuggable {


    override fun isGeneratedName(): Boolean = false
    override fun suggestedName(): String = "RimWorld"

    override fun getOptions(): ConfigurationOptions {
        return super.getOptions() as ConfigurationOptions
    }

    fun getScriptName(): String = options.getScriptName()
    fun setScriptName(scriptName: String?) = options.setScriptName(scriptName ?: "")

    fun getModListPath(): String = options.getModListPath()
    fun setModListPath(modListPath: String?) = options.setModListPath(modListPath ?: "")

    fun getSaveFilePath(): String = options.getSaveFilePath()
    fun setSaveFilePath(saveFilePath: String?) = options.setSaveFilePath(saveFilePath ?: "")

    fun getCommandLineOptions(): String = options.getCommandLineOptions()
    fun setCommandLineOptions(scriptName: String?) = options.setCommandLineOptions(scriptName ?: "")

    fun getEnvData(): Map<String, String> = options.getEnvironmentVariables()
    fun setEnvData(data: MutableMap<String, String>) = options.setEnvironmentVariables(data)

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState {
        return getRimworldState(environment)
    }

    @Suppress("UsagesOfObsoleteApi")
    @Deprecated("Please, override 'getRunProfileStateAsync' instead")
    override fun getStateAsync(
        executor: Executor,
        environment: ExecutionEnvironment
    ): Promise<RunProfileState> {
        @Suppress("DEPRECATION")
        throw UnsupportedOperationException(
            RiderRunBundle.message(
                "obsolete.synchronous.api.is.used.message",
                UnityExeConfiguration::getStateAsync.name
            )
        )
    }

    override suspend fun getRunProfileStateAsync(
        executor: Executor,
        environment: ExecutionEnvironment
    ): RunProfileState {
        // For older versions of Rimworld (pre-1.7) we needed to use Unity Doorstop to debug Rimworld. This meant
        // setting up doorstop files in the directory and manually attaching a debugger as a second process.
        if (ScopeHelper.findRimworldVersion(getScriptName())?.isOrGreaterThan(1, 7) != true) {
            return DoorstopRunState.create(
                getScriptName(),
                getModListPath(),
                getSaveFilePath(),
                getRimworldState(environment),
                environment
            )
        }
        return DirectAttachRunState.launch(
            createRimworldCommandLine(directLaunch = true),
            getModListPath(),
            getSaveFilePath(),
            environment
        )
    }

    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> {
        return RimworldDev.Rider.run.SettingsEditor(project)
    }

    private fun getRimworldState(environment: ExecutionEnvironment): CommandLineState {
        return object : CommandLineState(environment) {
            override fun startProcess(): ProcessHandler {
                val commandLine = createRimworldCommandLine(directLaunch = false)

                QuickStartUtils.setup(getModListPath(), getSaveFilePath())

                val processHandler = ProcessHandlerFactory.getInstance()
                    .createColoredProcessHandler(commandLine)

                processHandler.addProcessListener(createProcessListener(processHandler))
                ProcessTerminatedListener.attach(processHandler)
                return processHandler
            }
        }
    }

    /**
     * [directLaunch] runs the game binary itself even on macOS, where we'd otherwise go through `open`: `open` hands the
     * launch to launchd, so the process we start isn't the game and its PID is useless for attaching a debugger.
     */
    private fun createRimworldCommandLine(directLaunch: Boolean): GeneralCommandLine {
        val scriptName = getScriptName()
        val extraArgs = getCommandLineOptions().split(' ').filter { it.isNotEmpty() }

        val commandLine = when {
            OS.CURRENT == OS.macOS && !directLaunch -> {
                val params = if (extraArgs.isEmpty()) listOf(scriptName)
                             else listOf(scriptName, "--args") + extraArgs
                GeneralCommandLine("open").withParameters(params)
            }
            OS.CURRENT == OS.macOS && scriptName.endsWith(".app") -> {
                val binary = File(scriptName, "Contents/MacOS").listFiles()?.firstOrNull { it.isFile && it.canExecute() }
                GeneralCommandLine(binary?.absolutePath ?: scriptName).withParameters(extraArgs)
            }
            else -> GeneralCommandLine(scriptName).withParameters(extraArgs)
        }

        if (directLaunch) commandLine.withWorkDirectory(File(scriptName).parentFile)
        EnvironmentVariablesData.create(getEnvData(), true).configureCommandLine(commandLine, true)
        return commandLine
    }

    private fun createProcessListener(siblingProcessHandler: ProcessHandler?): ProcessListener {
        return object : ProcessAdapter() {
            override fun processTerminated(event: ProcessEvent) {
                val processHandler = event.processHandler
                processHandler.removeProcessListener(this)

                QuickStartUtils.tearDown(getSaveFilePath());
            }
        }
    }
}