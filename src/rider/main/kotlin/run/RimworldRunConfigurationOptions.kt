package RimworldDev.Rider.run

import com.intellij.execution.configurations.RunConfigurationOptions
import com.intellij.openapi.components.StoredProperty




class RimworldRunConfigurationOptions: RunConfigurationOptions() {

    private val myScriptName: StoredProperty<String?> = string("").provideDelegate(this, "scriptName")

    fun getScriptName(): String {
        return myScriptName.getValue(this) ?: ""
    }

    fun setScriptName(scriptName: String) {
        myScriptName.setValue(this, scriptName)
    }
}