package RimworldDev.Rider.run

import com.intellij.openapi.fileChooser.FileChooserDescriptorFactory
import com.intellij.openapi.options.SettingsEditor
import com.intellij.openapi.ui.TextFieldWithBrowseButton
import com.intellij.util.ui.FormBuilder
import javax.swing.JComponent
import javax.swing.JPanel


class SettingsEditor: SettingsEditor<RimworldRunConfiguration> {
    private var myPanel: JPanel;
    private var scriptPathField: TextFieldWithBrowseButton;

    constructor(): super() {
        scriptPathField = TextFieldWithBrowseButton()
        scriptPathField.addBrowseFolderListener("Select Script File", null, null,
        FileChooserDescriptorFactory.createSingleFileDescriptor());

        myPanel = FormBuilder.createFormBuilder().addLabeledComponent("Script file", scriptPathField).panel;
    }

    override fun resetEditorFrom(demoRunConfiguration: RimworldRunConfiguration) {
        scriptPathField.text = demoRunConfiguration.getScriptName()
    }

    override fun applyEditorTo(demoRunConfiguration: RimworldRunConfiguration) {
        demoRunConfiguration.setScriptName(scriptPathField.text)
    }

    override fun createEditor(): JComponent {
        return myPanel;
    }
}