package RimworldDev.Rider.run

import com.intellij.execution.Executor
import com.intellij.execution.configuration.EnvironmentVariablesData
import com.intellij.execution.configurations.*
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.execution.process.*
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.util.startOnUiAsync
import com.intellij.openapi.rd.util.toPromise
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.plugins.unity.run.configurations.UnityAttachToPlayerFactory
import com.jetbrains.rider.plugins.unity.run.configurations.UnityPlayerDebugConfigurationOptions
import com.jetbrains.rider.plugins.unity.run.configurations.UnityPlayerDebugConfigurationType
import com.jetbrains.rider.run.configurations.AsyncRunConfiguration
import com.jetbrains.rider.run.getProcess
import kotlinx.coroutines.ExperimentalCoroutinesApi
import org.jetbrains.concurrency.Promise


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

    @OptIn(ExperimentalCoroutinesApi::class)
    override fun getStateAsync(executor: Executor, environment: ExecutionEnvironment): Promise<RunProfileState> {
        val attachToDebugFactory = UnityAttachToPlayerFactory(UnityPlayerDebugConfigurationType())
        val attachToDebug = attachToDebugFactory.createTemplateConfiguration(project)
        attachToDebug.name = "Custom Player"

        val attachToDebugOptions = UnityPlayerDebugConfigurationOptions()
        attachToDebugOptions.playerId = "CustomPlayer(localhost:56000)"
        attachToDebugOptions.projectName = "Custom"

        attachToDebug.loadState(attachToDebugOptions)

        return environment.project.lifetime.startOnUiAsync {
            RunState(
                getScriptName(),
                getSaveFilePath(),
                getModListPath(),
                getRimworldState(environment),
                UnityDebugRemoteConfiguration(),
                environment,
                "CustomPlayer"
            )
        }.toPromise()
    }


    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> {
        return RimworldDev.Rider.run.SettingsEditor(project)
    }

    private fun getRimworldState(environment: ExecutionEnvironment): CommandLineState {
        return object : CommandLineState(environment) {
            override fun startProcess(): ProcessHandler {
                val commandLine = GeneralCommandLine(getScriptName())
                    .withParameters(getCommandLineOptions())

                EnvironmentVariablesData.create(getEnvData(), true).configureCommandLine(commandLine, true)

                QuickStartUtils.setup(getModListPath(), getSaveFilePath());

                val processHandler = ProcessHandlerFactory.getInstance()
                    .createColoredProcessHandler(commandLine)

                processHandler.addProcessListener(createProcessListener(processHandler))
                ProcessTerminatedListener.attach(processHandler)
                return processHandler
            }
        }
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