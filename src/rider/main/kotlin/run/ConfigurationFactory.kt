package RimworldDev.Rider.run

import com.intellij.execution.RunManager
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.openapi.project.Project

class ConfigurationFactory(type: ConfigurationType): ConfigurationFactory(type) {
    override fun createTemplateConfiguration(project: Project): RunConfiguration {
        return RimworldRunConfiguration(project, this, "Rimworld");
    }

    override fun createTemplateConfiguration(project: Project, runManager: RunManager): RunConfiguration {
        return RimworldRunConfiguration(project, this, "Rimworld");
    }

    override fun getId(): String {
        return "RimworldRunConfiguration";
    }
}