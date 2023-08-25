package RimworldDev.Rider.run

import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration

class UnityDebugRemoteConfiguration: RemoteConfiguration {
    override var address: String
        get() = "localhost"
        set(value) {}
    override var listenPortForConnections: Boolean
        get() = false
        set(value) {}
    override var port: Int
        get() = 56000
        set(value) {}
}