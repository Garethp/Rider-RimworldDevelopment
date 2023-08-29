package RimworldDev.Rider.run

import com.intellij.execution.configurations.RunConfigurationOptions
import com.intellij.openapi.components.StoredProperty

class ConfigurationOptions : RunConfigurationOptions() {

    private val scriptName: StoredProperty<String?> = string("").provideDelegate(this, "scriptName")
    private val commandLineOptions: StoredProperty<String?> = string("").provideDelegate(this, "commandLineOptions")
    private val environmentVariables: StoredProperty<MutableMap<String, String>> =
        map<String, String>().provideDelegate(this, "environmentVariables")

    fun getScriptName(): String {
        return scriptName.getValue(this) ?: ""
    }

    fun setScriptName(scriptName: String) {
        this.scriptName.setValue(this, scriptName)
    }

    fun getCommandLineOptions(): String {
        return commandLineOptions.getValue(this) ?: ""
    }

    fun setCommandLineOptions(commandLineOptions: String) {
        this.commandLineOptions.setValue(this, commandLineOptions)
    }

    fun getEnvironmentVariables(): Map<String, String> {
        return environmentVariables.getValue(this).toMap()
    }

    fun setEnvironmentVariables(data: MutableMap<String, String>) {
        this.environmentVariables.setValue(this, data)

    }
}