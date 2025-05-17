package RimworldDev.Rider.remodder

import com.intellij.openapi.components.*

@Service(Service.Level.PROJECT)
@State(name = "RemodderSettings", storages = [Storage(StoragePathMacros.WORKSPACE_FILE)])
class RemodderStateComponent : SimplePersistentStateComponent<RemodderState>(RemodderState())

class RemodderState : BaseState() {
    var userAssemblies by list<String>()
}
