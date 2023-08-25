package RimworldDev.Rider.run

import com.intellij.execution.RunManager
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.openapi.components.BaseState
import com.intellij.openapi.project.Project

class ConfigurationFactory(type: ConfigurationType): ConfigurationFactory(type) {
    override fun createTemplateConfiguration(project: Project): RunConfiguration {
        return RunConfiguration(project, this, "Rimworld")
    }

    override fun createTemplateConfiguration(project: Project, runManager: RunManager): RunConfiguration {
        return RunConfiguration(project, this, "Rimworld")
    }

    override fun getOptionsClass(): Class<out BaseState> {
        return ConfigurationOptions::class.java
    }

    override fun getId(): String {
        return "RimworldRunConfiguration"
    }
}