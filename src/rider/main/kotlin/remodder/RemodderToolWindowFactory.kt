package RimworldDev.Rider.remodder

import com.intellij.diff.DiffContentFactory
import com.intellij.diff.DiffManager
import com.intellij.diff.requests.SimpleDiffRequest
import com.intellij.diff.util.DiffUserDataKeys
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.TextEditor
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.DialogBuilder
import com.intellij.openapi.ui.Messages
import com.intellij.openapi.wm.ToolWindow
import com.intellij.openapi.wm.ToolWindowFactory
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiManager
import com.intellij.psi.impl.source.tree.LeafPsiElement
import com.intellij.psi.util.childrenOfType
import com.intellij.psi.util.descendantsOfType
import com.intellij.psi.util.parentOfType
import com.intellij.psi.util.parentsOfType
import com.intellij.ui.CollectionListModel
import com.intellij.ui.ToolbarDecorator
import com.intellij.ui.components.JBList
import com.intellij.ui.content.ContentFactory
import com.jetbrains.rd.platform.util.toPromise
import com.jetbrains.rider.languages.fileTypes.csharp.kotoparser.lexer.CSharpTokenType
import com.jetbrains.rider.languages.fileTypes.csharp.psi.CSharpNamespaceDeclaration
import com.jetbrains.rider.languages.fileTypes.csharp.psi.impl.CSharpDeclarationIdentifier
import com.jetbrains.rider.languages.fileTypes.csharp.psi.impl.CSharpDummyDeclaration
import com.jetbrains.rider.languages.fileTypes.csharp.psi.impl.CSharpNamespaceFileScopeHeader
import com.jetbrains.rider.plugins.rdprotocol.remodderProtocolModel
import com.jetbrains.rider.projectView.solution
import java.awt.BorderLayout
import java.awt.FlowLayout
import javax.swing.JButton
import javax.swing.JLabel
import javax.swing.JPanel
import javax.swing.JTextArea

class RemodderToolWindowFactory : ToolWindowFactory {
    private var errorMsg: String = ""

    override fun createToolWindowContent(project: Project, toolWindow: ToolWindow) {
        val diffPanel = DiffManager.getInstance().createRequestPanel(project, toolWindow.disposable, null)

        val refresh = JButton("Refresh")
        val showUserAssemblies = JButton("User assemblies")
        val statusLabel = JLabel()
        val errorDetails = JButton("See error")
        errorDetails.isVisible = false

        refresh.addActionListener {
            val userAssemblies = project.getService<RemodderStateComponent>(RemodderStateComponent::class.java).state.userAssemblies
            val editor = (FileEditorManager.getInstance(project).selectedEditor as TextEditor).editor
            val view = PsiManager.getInstance(project).findViewProvider(editor.virtualFile)
            val el = view?.findElementAt(editor.caretModel.offset) ?: return@addActionListener

            val filePath = editor.virtualFile.path
            val typeName = namespaceAndClassOfElement(el) ?: "<null>"

            statusLabel.text = "$typeName..."
            errorDetails.isVisible = false

            project.solution.remodderProtocolModel.decompile.start(arrayOf(filePath, typeName) + userAssemblies).toPromise().then {
                if (it.size == 1)
                {
                    statusLabel.text = "$typeName: ${it[0]}"
                    errorDetails.isVisible = false
                    return@then
                }

                val content1 = DiffContentFactory.getInstance().create(project, it[0], editor.virtualFile)
                val content2 = DiffContentFactory.getInstance().create(project, it[1], editor.virtualFile)
                content2.putUserData(DiffUserDataKeys.FORCE_READ_ONLY, true)

                val request = SimpleDiffRequest("Original/Transpiled", content1, content2, "Original", "Transpiled")
                diffPanel.setRequest(request)

                statusLabel.text = typeName
                errorDetails.isVisible = false
            }.onError {
                errorMsg = it.toString()
                statusLabel.text = "$typeName: ERROR"
                errorDetails.isVisible = true
            }
        }

        errorDetails.addActionListener {
            val errorTextArea = JTextArea()
            errorTextArea.text = errorMsg
            errorTextArea.isEditable = false

            val dialogBuilder = DialogBuilder(project)
            dialogBuilder.setCenterPanel(errorTextArea)
            dialogBuilder.show()
        }

        showUserAssemblies.addActionListener {
            val userAssemblies = project.getService<RemodderStateComponent>(RemodderStateComponent::class.java).state.userAssemblies
            val listModel = CollectionListModel(userAssemblies, true)
            val jbList = JBList(listModel)

            val dialogBuilder = DialogBuilder(project)

            dialogBuilder.setCenterPanel(jbList)
            dialogBuilder.setNorthPanel(ToolbarDecorator.createDecorator(jbList).setAddAction {
                val str = Messages.showInputDialog(project, "Add user assembly path", "User Assembly Path", null)
                if (str != null)
                    listModel.add(str)
            }.setRemoveAction {
                listModel.remove(jbList.selectedValue)
            }.createPanel())

            dialogBuilder.show()
        }

        val mainPanel = JPanel(BorderLayout(0, 1))
        mainPanel.add(diffPanel.component, BorderLayout.CENTER)

        val leftPanel = JPanel(FlowLayout(FlowLayout.LEFT))
        leftPanel.add(refresh)
        leftPanel.add(showUserAssemblies)
        leftPanel.add(statusLabel)
        leftPanel.add(errorDetails)
        mainPanel.add(leftPanel, BorderLayout.NORTH)

        val content = ContentFactory.getInstance().createContent(mainPanel, "Transpiler Preview", false)

        toolWindow.contentManager.addContent(content)
    }

    private fun namespaceAndClassOfElement(psiElement: PsiElement): String? {
        // Simple heuristics to get <namespace>.<class> of code under caret
        val nsDecl = psiElement.parentOfType<CSharpNamespaceDeclaration>()
        var nsIdent = nsDecl?.childrenOfType<CSharpDeclarationIdentifier>()?.firstOrNull()

        if (nsIdent == null) {
            nsIdent = psiElement.containingFile.
            descendantsOfType<CSharpNamespaceFileScopeHeader>().firstOrNull()?.
            childrenOfType<CSharpDeclarationIdentifier>()?.firstOrNull()
        }

        val classDecl = psiElement.parentsOfType<CSharpDummyDeclaration>().filter {
            it.childrenOfType<LeafPsiElement>().any { it.elementType == CSharpTokenType.CLASS_KEYWORD }
        }.firstOrNull()
        val classIdent = classDecl?.childrenOfType<CSharpDeclarationIdentifier>()?.firstOrNull()

        return if (nsIdent != null)
            nsIdent.text + "." + classIdent?.text
        else
            classIdent?.text
    }
}