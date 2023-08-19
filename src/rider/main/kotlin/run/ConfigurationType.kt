package RimworldDev.Rider.run

import com.intellij.execution.configurations.ConfigurationTypeBase
import com.intellij.icons.AllIcons
import com.intellij.openapi.util.NotNullLazyValue
import javax.swing.Icon


class ConfigurationType : ConfigurationTypeBase {
    constructor() : super("RimworldRunConfiguration", "Rimworld", "Rimworld Run Configuration", NotNullLazyValue.createValue<Icon> { AllIcons.Nodes.Console }) {
        addFactory(ConfigurationFactory(this))
    }
}