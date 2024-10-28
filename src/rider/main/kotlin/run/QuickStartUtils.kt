package RimworldDev.Rider.run

import com.intellij.util.io.delete
import org.apache.commons.lang3.SystemUtils
import java.nio.file.Path
import kotlin.io.path.*

class QuickStartUtils {
    companion object {
        fun getLudeonPath(): Path? {
            val possiblePaths = listOfNotNull(
                System.getenv("APPDATA")?.let{ Path(it, "..", "LocalLow", "Ludeon Studios", "RimWorld by Ludeon Studios") },
                Path(SystemUtils.getUserHome().absolutePath, ".config", "unity3d", "Ludeon Studios", "RimWorld by Ludeon Studios"),
            )

            return possiblePaths.firstOrNull { path: Path -> path.exists() }
        }

        fun setup(modListPath: String, saveFilePath: String) {
            val location = getLudeonPath() ?: return

            val configLocation = Path(location.absolutePathString(), "Config")
            val autostartFile = Path(location.absolutePathString(), "Saves", "autostart.rws")
            val saveFile = Path(saveFilePath)
            val modListFile = Path(modListPath)

            if (!modListFile.isRegularFile()) return;
            if (!configLocation.isDirectory()) return;

            if (saveFile.isRegularFile()) {
                if (autostartFile.isRegularFile()) {
                    autostartFile.copyTo(Path(location.absolutePathString(), "Saves", "autostart.rws.rider-bak"), true)
                }

                saveFile.copyTo(autostartFile, true)
            }

            val existingFile = Path(configLocation.absolutePathString(), "ModsConfig.xml").toFile()
            existingFile.copyTo(Path(configLocation.absolutePathString(), "ModsConfig.xml.rider-bak").toFile(), true)

            modListFile.toFile().copyTo(existingFile, true)
        }

        fun tearDown(saveFilePath: String) {
            Thread.sleep(50)
            val location = getLudeonPath() ?: return

            val configLocation = Path(location.absolutePathString(), "Config")
            val backupFile = Path(configLocation.absolutePathString(), "ModsConfig.xml.rider-bak")
            val autostartFile = Path(location.absolutePathString(), "Saves", "autostart.rws")
            val autostartBackupFile = Path(location.absolutePathString(), "Saves", "autostart.rws.rider-bak")

            if (backupFile.isRegularFile() && backupFile.exists()) {
                backupFile.copyTo(Path(configLocation.absolutePathString(), "ModsConfig.xml"), true)
                backupFile.delete(false)
            }

            if (Path(saveFilePath).exists() && autostartFile.isRegularFile() && autostartFile.exists()) {
                autostartFile.deleteExisting()
            }

            if (autostartBackupFile.isRegularFile() && autostartBackupFile.exists()) {
                autostartBackupFile.copyTo(autostartFile)
            }
        }
    }
}
