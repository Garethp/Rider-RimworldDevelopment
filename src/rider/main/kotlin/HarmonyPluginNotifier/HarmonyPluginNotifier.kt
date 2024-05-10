package RimworldDev.Rider.HarmonyPluginNotifier

import com.intellij.ide.IdeBundle
import com.intellij.ide.plugins.PluginManagerCore
import com.intellij.ide.plugins.advertiser.PluginData
import com.intellij.openapi.extensions.PluginId
import com.intellij.openapi.fileEditor.FileEditor
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.StartupActivity
import com.intellij.openapi.updateSettings.impl.pluginsAdvertisement.FUSEventSource
import com.intellij.openapi.updateSettings.impl.pluginsAdvertisement.installAndEnable
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.EditorNotificationPanel
import com.intellij.ui.EditorNotificationProvider
import java.util.function.Function
import javax.swing.JComponent
import javax.swing.JLabel

class HarmonyPluginNotifier : EditorNotificationProvider {
    override fun collectNotificationData(p0: Project, p1: VirtualFile): Function<in FileEditor, out JComponent?>? {


        return Function { editor ->
            val descriptorsById = PluginManagerCore.buildPluginIdMap()
            val hasPlugin = descriptorsById.containsKey(
                PluginId.getId("com.zetrith.remodder")
            )

            apply(editor, p0)
        }
    }

    fun apply(editor: FileEditor, project: Project): EditorNotificationPanel? {
        if (editor.file.extension != "cs") return null
        if (!editor.file.contentsToByteArray().toString(Charsets.UTF_8).contains(Regex("using\\s+HarmonyLib;"))) return null

        lateinit var label: JLabel
        val panel = object : EditorNotificationPanel(editor, EditorNotificationPanel.Status.Info) {
            init {
                label = myLabel
            }
        }

        panel.text = IdeBundle.message("plugins.advertiser.plugins.found", "harmony")

        fun createInstallActionLabel() {
            val labelText =
                IdeBundle.message("plugins.advertiser.action.install.plugin.name", "Remodder")

            panel.createActionLabel(labelText) {
                FUSEventSource.EDITOR.logInstallPlugins(listOf("com.zetrith.remodder"))
                installAndEnable(project, setOf(PluginId.getId("com.zetrith.remodder")), true) {
//                    pluginAdvertiserExtensionsState.addEnabledExtensionOrFileNameAndInvalidateCache(extensionOrFileName)
//                    updateAllNotifications(project)
                }
            }

            panel.createActionLabel(IdeBundle.message("plugins.advertiser.action.ignore.extension")) {
//                FUSEventSource.EDITOR.logIgnoreExtension(project)
//                pluginAdvertiserExtensionsState.ignoreExtensionOrFileNameAndInvalidateCache(extensionOrFileName)
//                updateAllNotifications(project)
            }
        }

        createInstallActionLabel()

        return panel
    }
}