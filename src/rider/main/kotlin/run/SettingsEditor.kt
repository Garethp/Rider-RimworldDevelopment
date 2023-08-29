package RimworldDev.Rider.run

import com.intellij.execution.configuration.EnvironmentVariablesComponent
import com.intellij.openapi.fileChooser.FileChooserDescriptorFactory
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.ui.TextFieldWithBrowseButton
import com.intellij.ui.RawCommandLineEditor
import com.intellij.util.ui.FormBuilder
import javax.swing.JComponent
import javax.swing.JPanel


class SettingsEditor : SettingsEditor<RunConfiguration>() {
    private var myPanel: JPanel = JPanel()
    private val exePath: TextFieldWithBrowseButton = TextFieldWithBrowseButton()
    private val commandLineOptions: RawCommandLineEditor = RawCommandLineEditor()
    private val environmentVariables: EnvironmentVariablesComponent = EnvironmentVariablesComponent()

    init {
        exePath.addBrowseFolderListener(
            "Executable Path",
            "",
            null,
            FileChooserDescriptorFactory.createSingleFileDescriptor()
        )

        myPanel = FormBuilder
            .createFormBuilder()
            .addLabeledComponent("RimWorld path:", exePath)
            .addLabeledComponent("Program arguments:", commandLineOptions)
            .addLabeledComponent("Environment variables:", environmentVariables.component)
            .panel
    }

    override fun resetEditorFrom(configuration: RunConfiguration) {
        exePath.text = configuration.getScriptName()
        commandLineOptions.text = configuration.getCommandLineOptions()
        environmentVariables.envs = configuration.getEnvData()
    }

    override fun applyEditorTo(configuration: RunConfiguration) {
        configuration.setScriptName(exePath.text)

        configuration.setEnvData(environmentVariables.envData.envs)
        configuration.setCommandLineOptions(commandLineOptions.text)
    }

    override fun createEditor(): JComponent {
        return myPanel
    }
}