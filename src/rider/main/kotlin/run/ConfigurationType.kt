package RimworldDev.Rider.run

import RimworldDev.Rider.PluginIcons
import com.intellij.execution.configurations.ConfigurationTypeBase
import com.intellij.openapi.util.NotNullLazyValue


class ConfigurationType : ConfigurationTypeBase(
    "RimworldRunConfiguration",
    "Rimworld",
    "Rimworld Run Configuration",
    NotNullLazyValue.createValue { PluginIcons.RIMWORLD }) {
    init {
        addFactory(ConfigurationFactory(this))
    }
}