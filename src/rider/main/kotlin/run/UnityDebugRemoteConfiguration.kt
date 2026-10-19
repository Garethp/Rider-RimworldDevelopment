package RimworldDev.Rider.run

import com.jetbrains.rider.run.configurations.remote.RemoteConfiguration
import java.io.IOException
import java.net.InetAddress
import java.net.ServerSocket

class UnityDebugRemoteConfiguration(private val debugPort: Int) : RemoteConfiguration {
    override var address: String
        get() = host
        set(value) {}
    override var listenPortForConnections: Boolean
        get() = false
        set(value) {}
    override var port: Int
        get() = debugPort
        set(value) {}

    companion object {

        const val host = "127.0.0.1"

        // We're trying to avoid ports that most OS' or developer tools will block off.
        // Since RimWorld's Steam ID is 294100, this felt fitting
        private const val defaultPort = 29410

        // We already had one issue where the port we HAD picked was one that gets reserved by Windows Hyper-V when
        // you were using WSL, so rather than just assuming that another port we pick will be free, we'll scan up to 100
        // ports to find a free one.
        fun findAvailableDebugPort(): Int {
            val host = InetAddress.getByName(host)
            for (port in defaultPort until defaultPort + 100) {
                try {
                    ServerSocket(port, 1, host).close()
                    return port
                } catch (_: IOException) {
                }
            }

            return ServerSocket(0, 1, host).use { it.localPort }
        }
    }
}
