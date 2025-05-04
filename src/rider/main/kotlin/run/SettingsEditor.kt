package RimworldDev.Rider.run

import com.intellij.execution.configuration.EnvironmentVariablesComponent
import com.intellij.openapi.fileChooser.FileChooserDescriptorFactory
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.TextFieldWithBrowseButton
import com.intellij.ui.RawCommandLineEditor
import com.intellij.util.ui.FormBuilder
import javax.swing.JComponent
import javax.swing.JPanel


class SettingsEditor(project: Project) : SettingsEditor<RunConfiguration>() {
    private var myPanel: JPanel = JPanel()
    private val exePath: TextFieldWithBrowseButton = TextFieldWithBrowseButton()
    private val commandLineOptions: RawCommandLineEditor = RawCommandLineEditor()
    private val environmentVariables: EnvironmentVariablesComponent = EnvironmentVariablesComponent()
    private val modListPath: TextFieldWithBrowseButton = TextFieldWithBrowseButton()
    private val saveFilePath: TextFieldWithBrowseButton = TextFieldWithBrowseButton()

    init {
        exePath.addBrowseFolderListener(
            null,
            FileChooserDescriptorFactory.singleFile().withTitle("Executable Path").withDescription("")
        )

        modListPath.addBrowseFolderListener(
            project,
            FileChooserDescriptorFactory.singleFile().withTitle("Modlist Path").withDescription("")
        )

        saveFilePath.addBrowseFolderListener(
            project,
            FileChooserDescriptorFactory.singleFile().withTitle("Save File Path").withDescription("")
        )

        myPanel = FormBuilder
            .createFormBuilder()
            .addLabeledComponent("RimWorld path:", exePath)
            .addLabeledComponent("Mod list path:", modListPath)
            .addLabeledComponent("Save file path:", saveFilePath)
            .addLabeledComponent("Program arguments:", commandLineOptions)
            .addLabeledComponent("Environment variables:", environmentVariables.component)
            .panel
    }

    override fun resetEditorFrom(configuration: RunConfiguration) {
        exePath.text = configuration.getScriptName()
        modListPath.text = configuration.getModListPath()
        saveFilePath.text = configuration.getSaveFilePath()
        commandLineOptions.text = configuration.getCommandLineOptions()
        environmentVariables.envs = configuration.getEnvData()
    }

    override fun applyEditorTo(configuration: RunConfiguration) {
        configuration.setScriptName(exePath.text)
        configuration.setModListPath(modListPath.text.replace('\\', '/'))
        configuration.setSaveFilePath(saveFilePath.text.replace('\\', '/'))

        configuration.setEnvData(environmentVariables.envData.envs)
        configuration.setCommandLineOptions(commandLineOptions.text)
    }

    override fun createEditor(): JComponent {
        return myPanel
    }
}