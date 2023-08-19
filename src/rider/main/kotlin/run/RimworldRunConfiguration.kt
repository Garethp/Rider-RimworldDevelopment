package RimworldDev.Rider.run

import com.intellij.execution.Executor
import com.intellij.execution.RunManager
import com.intellij.execution.configurations.*
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.process.ProcessHandler
import com.intellij.execution.process.ProcessHandlerFactory
import com.intellij.execution.process.ProcessTerminatedListener
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.jetbrains.rider.debugger.IRiderDebuggable
import com.jetbrains.rider.plugins.unity.run.configurations.*
import com.jetbrains.rider.run.configurations.AsyncRunConfiguration
import org.jetbrains.concurrency.Promise


class RimworldRunConfiguration(project: Project, factory: ConfigurationFactory, name: String) : RunConfigurationBase<RimworldRunConfigurationOptions>(project, factory, name), RunConfigurationWithSuppressedDefaultDebugAction, AsyncRunConfiguration, IRiderDebuggable {

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState {
        val attachToDebugFactory = UnityAttachToPlayerFactory(UnityPlayerDebugConfigurationType());
        val attachToDebug = attachToDebugFactory.createTemplateConfiguration(project);
        attachToDebug.name = "Custom Player";

        val attachToDebugOptions = UnityPlayerDebugConfigurationOptions();
        attachToDebugOptions.playerId = "CustomPlayer(localhost:56000)";
        attachToDebugOptions.projectName = "Custom";

        attachToDebug.loadState(attachToDebugOptions);

        getRimworldState(environment).execute(executor, environment.runner);

        return attachToDebug.getState(executor, environment);
    }

    override fun getStateAsync(executor: Executor, environment: ExecutionEnvironment): Promise<RunProfileState> {
        val attachToDebugFactory = UnityAttachToPlayerFactory(UnityPlayerDebugConfigurationType());
        val attachToDebug = attachToDebugFactory.createTemplateConfiguration(project);
        attachToDebug.name = "Custom Player";

        val attachToDebugOptions = UnityPlayerDebugConfigurationOptions();
        attachToDebugOptions.playerId = "CustomPlayer(localhost:56000)";
        attachToDebugOptions.projectName = "Custom";

        attachToDebug.loadState(attachToDebugOptions);

        getRimworldState(environment).execute(executor, environment.runner);
        return attachToDebug.getStateAsync(executor, environment);
    }



    override fun getConfigurationEditor(): SettingsEditor<out RunConfiguration> {
        return RimworldDev.Rider.run.SettingsEditor();
    }

    private fun getRimworldState(environment: ExecutionEnvironment): RunProfileState {
        return object : CommandLineState(environment) {
            override fun startProcess(): ProcessHandler {
                val commandLine = GeneralCommandLine("D:\\SteamLibrary\\steamapps\\common\\RimWorld\\RimWorldWin64.exe")
                val processHandler = ProcessHandlerFactory.getInstance()
                        .createColoredProcessHandler(commandLine)
                ProcessTerminatedListener.attach(processHandler)
                return processHandler
            }
        }
    }
}