package RimworldDev.Rider

import com.jetbrains.rider.editorActions.RiderEditorActionPolicy
import com.jetbrains.rider.editorActions.RiderTypingPolicy

class XmlTypingPolicy : RiderTypingPolicy {
    override fun forceFrontendExecution(): Boolean {
        return true
    }
}

class XmlEditorActionPolicy : RiderEditorActionPolicy {
    override fun forceFrontendExecution(): Boolean {
        return true
    }
}