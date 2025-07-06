package RimworldDev.Rider.helpers

import java.io.File
import java.nio.file.Files
import javax.xml.parsers.DocumentBuilderFactory

class ScopeHelper {
    companion object {
        fun getSteamLocations(): List<String> {
            var home = System.getProperty("user.home")

            var locations = mutableListOf<String>(
                "C:\\Program Files (x86)\\Steam\\steamapps\\",
                "C:\\Program Files\\Steam\\steamapps\\",
                "${home}/steam/steam/steamapps/",
                "${home}/snap/steam/common/.local/share/Steam/steamapps/",
                "${home}/.local/share/Steam/steamapps/",
                "${home}/.steam/debian-installation/steamapps/common/",
            )

            locations.addAll(File.listRoots().map {
                "${it.path}/SteamLibrary/steamapps"
            })

            return locations.filter { location -> File(location).exists() }.map {
                File(it).absoluteFile.toString()
            }
        }

        fun findRimworldDirectory(): String? {
            // @TODO: Check the user settings for the Rimworld location

            var locations = mutableListOf<String>();
            locations.addAll(getSteamLocations().filter {
                File("${it}/common/Rimworld").exists()
            }.map {
                File("${it}/common/Rimworld").absolutePath.toString()
            })

            if (locations.isNotEmpty()) return locations.first()

            // @TODO: Check from the current directory

            return null
        }

        fun findModDirectories(): List<String> {
            val locations = mutableListOf<String>()
            val rimworldLocation = findRimworldDirectory()
            if (rimworldLocation != null) {
                var dataDirectory = File("$rimworldLocation/Data")
                var modsDirectory = File("$rimworldLocation/Mods")

                if (dataDirectory.exists()) locations.add(dataDirectory.absolutePath)
                if (modsDirectory.exists()) locations.add(modsDirectory.absolutePath)
            }

            locations.addAll(getSteamLocations()
                .map { "${it}/workshop/content/294100/" }
                .filter { File(it).exists() }
                .map { File(it).absolutePath }
            )

            return locations
        }

        fun findModLocation(id: String): String? {
            val directoriesToCheck = findModDirectories()

            val xmlReaderFactory = DocumentBuilderFactory.newInstance()
            val builder = xmlReaderFactory.newDocumentBuilder()
            for (directory in directoriesToCheck) {
                for (child in File(directory).listFiles()) {
                    if (!child.isDirectory) continue

                    var aboutFile = File("${child.absolutePath}/About/About.xml")
                    if (!aboutFile.exists()) continue

                    val document = builder.parse(aboutFile)

                    var a = 1 + 1
                }
            }

            return null
        }

        fun findInstalledHarmonyDll(): String? {
            val version = findRimworldVersion()
            if (version == null) return null

            val directoriesToCheck = findModDirectories()

            val xmlReaderFactory = DocumentBuilderFactory.newInstance()
            val builder = xmlReaderFactory.newDocumentBuilder()
            for (directory in directoriesToCheck) {
                for (child in File(directory).listFiles()) {
                    if (!child.isDirectory) continue

                    var aboutFile = File("${child.absolutePath}/About/About.xml")
                    if (!aboutFile.exists()) continue

                    if (child.name != "2009463077") continue

                    if (File("${child.absolutePath}/${version}/Assemblies/0Harmony.dll").exists())
                        return File("${child.absolutePath}/${version}/Assemblies/0Harmony.dll").absolutePath

                    return File("${child.absolutePath}/Current/Assemblies/0Harmony.dll").absolutePath
                }
            }

            return null
        }

        fun findRimworldVersion(): String? {
            val directory = findRimworldDirectory()
            if (directory == null) return null

            if (!File("${directory}/Version.txt").exists()) return null

            val contents = File("${directory}/Version.txt").readText()

            return contents.substring(0, 3)
        }
    }
}