package rimworlddev.gradle

import groovy.json.JsonSlurper
import org.gradle.api.DefaultTask
import org.gradle.api.GradleException
import org.gradle.api.file.DirectoryProperty
import org.gradle.api.file.RegularFileProperty
import org.gradle.api.provider.Property
import org.gradle.api.tasks.Input
import org.gradle.api.tasks.Internal
import org.gradle.api.tasks.TaskAction
import org.gradle.api.tasks.options.Option
import org.gradle.process.ExecOperations
import org.w3c.dom.Document
import org.w3c.dom.Element
import java.io.ByteArrayOutputStream
import java.io.File
import java.net.URI
import java.net.URLDecoder
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse
import java.nio.file.Files
import java.nio.file.StandardCopyOption
import java.time.Duration
import java.util.zip.ZipFile
import javax.inject.Inject
import javax.xml.parsers.DocumentBuilderFactory
import javax.xml.transform.OutputKeys
import javax.xml.transform.TransformerFactory
import javax.xml.transform.dom.DOMSource
import javax.xml.transform.stream.StreamResult

/**
 * Runs a ReSharper plugin in an experimental Visual Studio instance (`devenv /rootSuffix`), for any plugin built on the
 * ReSharper SDK: everything project-specific is configured on the task (see [USAGE]).
 *
 * The first run sets the instance up: downloads the ReSharper installer matching [sdkVersion], installs ReSharper into the
 * instance, registers the plugin in the instance's packages.config, points `HostFullIdentifier` in `<project>.csproj.user`
 * at it, packs the plugin and unpacks the package into %LOCALAPPDATA%\JetBrains\plugins, and runs the installer again so
 * the instance picks the plugin up. Every run then rebuilds the plugin (the SDK's CopyPlugin step copies it into the
 * instance) and launches Visual Studio with ReSharper's internal mode and trace logging, waiting for it to exit.
 *
 * Builds use `dotnet msbuild` and the .csproj.user condition is 'Core': CopyPlugin resolves the project's references,
 * which are .NET assemblies for current SDKs, and Visual Studio's .NET Framework MSBuild can't load them
 * (BadImageFormatException on System.Runtime in InstalledProductsDiscoveryTask).
 */
abstract class RunVisualStudioTask : DefaultTask() {
    @get:Inject
    abstract val execOperations: ExecOperations

    /** The plugin's ReSharper project; its .csproj.user carries HostFullIdentifier. */
    @get:Internal
    abstract val projectFile: RegularFileProperty

    /** NuGet package id of the plugin (the project's PackageId). */
    @get:Input
    abstract val pluginId: Property<String>

    /** ReSharper SDK version in NuGet form (2026.1.5.2, 2026.3.0-eap02); picks the ReSharper installer. */
    @get:Input
    abstract val sdkVersion: Property<String>

    @get:Input
    @get:Option(option = "root-suffix", description = "Experimental Visual Studio instance (devenv /rootSuffix).")
    abstract val rootSuffix: Property<String>

    @get:Input
    @get:Option(option = "plugin-version", description = "Version the plugin package is installed as. Default: 9999.0.0.")
    abstract val pluginVersion: Property<String>

    /** Build configuration passed to MSBuild. Default: Debug. */
    @get:Input
    abstract val configuration: Property<String>

    /** Where downloaded ReSharper installers are cached (several GB each). */
    @get:Internal
    abstract val installerDirectory: DirectoryProperty

    /** Where the plugin package is packed to during setup. */
    @get:Internal
    abstract val packageOutputDirectory: DirectoryProperty

    /** ReSharper's log (devenv /ReSharper.LogFile). */
    @get:Internal
    abstract val logFile: RegularFileProperty

    @get:Input
    @get:Option(option = "plan", description = "Show what would happen, including what --clean/--reinstall would remove; change nothing.")
    abstract val plan: Property<Boolean>

    @get:Input
    @get:Option(option = "clean", description = "Remove the experimental instance and everything set up for it (keeps downloaded installers), then stop.")
    abstract val clean: Property<Boolean>

    @get:Input
    @get:Option(option = "clean-installers", description = "Like --clean, and also delete the downloaded ReSharper installers.")
    abstract val cleanInstallers: Property<Boolean>

    @get:Input
    @get:Option(option = "reinstall", description = "--clean, then set the instance up from scratch and launch.")
    abstract val reinstall: Property<Boolean>

    @get:Input
    @get:Option(option = "usage", description = "Print what the task does and its options.")
    abstract val usage: Property<Boolean>

    init {
        pluginVersion.convention("9999.0.0")
        configuration.convention("Debug")
        plan.convention(false)
        clean.convention(false)
        cleanInstallers.convention(false)
        reinstall.convention(false)
        usage.convention(false)
    }

    @TaskAction
    fun run() {
        if (usage.get()) {
            print(USAGE)
            return
        }
        if (!System.getProperty("os.name").startsWith("Windows", ignoreCase = true))
            throw GradleException("$name needs Windows (Visual Studio + ReSharper).")

        val suffix = rootSuffix.orNull?.trim().orEmpty()
        // An empty suffix would make the experimental instance's names match the normal ReSharper install's.
        if (suffix.isEmpty()) throw GradleException("$name needs a root suffix: set rootSuffix in the build script or pass --root-suffix.")
        val project = required(projectFile, "projectFile")
        val id = pluginId.get()
        val version = pluginVersion.get()
        val vs = findVisualStudio()
        val instance = Instance(vs, suffix, id, version, project, required(logFile, "logFile"),
            File(required(packageOutputDirectory, "packageOutputDirectory"), "$id.$version.nupkg"))

        val cleaning = clean.get() || cleanInstallers.get() || reinstall.get()
        val stopAfterClean = (clean.get() || cleanInstallers.get()) && !reinstall.get()

        if (plan.get()) {
            print(plan(instance, cleaning, stopAfterClean))
            return
        }

        if (cleaning) {
            ensureNotRunning(suffix)
            cleanSteps(instance, cleanInstallers.get()).forEach { step ->
                println("- ${step.description}")
                step.action()
            }
            println("Removed the experimental instance ${instance.hostId}.")
            if (stopAfterClean) return
        }

        val problem = setupProblem(instance)
        if (problem != null) {
            ensureNotRunning(suffix)
            println("Setting up the experimental instance: $problem")
            instance.userFile.delete()
            install(instance)
        } else if (instance.userFile.readText().contains("== 'Full'")) {
            // Set up before builds moved to dotnet: only the .csproj.user condition needs updating
            instance.userFile.writeText(instance.userFile.readText().replace("== 'Full'", "== 'Core'"))
            println("Updated ${instance.userFile} to deploy from dotnet builds")
        }

        exec("dotnet", "msbuild", "/t:Restore;Rebuild", instance.projectFile.path, "/v:minimal",
            "/p:Configuration=${configuration.get()}")
        exec(vs.devenv.path, "/rootSuffix", suffix, "/ReSharper.Internal",
            "/ReSharper.LogFile", instance.logFile.path, "/ReSharper.LogLevel", "Trace")
    }

    // --- The experimental instance -------------------------------------------------------------------------------------

    /** Everything that belongs to one experimental instance: `ReSharperPlatformVs<major>_<VS instance id><suffix>`. */
    private inner class Instance(
        val vs: VisualStudio, val suffix: String, val id: String, val version: String,
        val projectFile: File, val logFile: File, val packedPlugin: File,
    ) {
        val userFile = File(projectFile.path + ".user")
        val platform = "ReSharperPlatformVs${vs.majorVersion}"
        val hostId = "${platform}_${vs.instanceId}$suffix"
        val installFolder = File(localAppData(), "JetBrains/Installations/$hostId")
        val uninstaller = File(installFolder, "JetBrains.Platform.Installer.exe")
        val pluginPackage = File(localAppData(), "JetBrains/plugins/$id.$version")
        val visualStudioData = File(localAppData(), "Microsoft/VisualStudio/${vs.majorVersion}.0_${vs.instanceId}$suffix")
        val visualStudioSettings = File(localAppData(), "Microsoft/VisualStudio/$suffix/SettingsV2.${vs.majorVersion}")

        /** `v261_<id><suffix>`, `vAny_<id><suffix>`: exactly this instance, never a longer suffix or the normal install. */
        fun owns(name: String) = name.startsWith("v") && name.substringAfter('_', "") == "${vs.instanceId}$suffix"

        private fun owned(parent: File) = parent.listFiles { f -> f.isDirectory && owns(f.name) }.orEmpty().toList()
        val settingsDirectories get() = owned(File(localAppData(), "JetBrains/$platform"))
        val transientDirectories get() = owned(File(localAppData(), "JetBrains/Transient/$platform"))

        /** The instance's NuGet.Config folder; present once ReSharper is installed into it. */
        val hive get() = settingsDirectories.firstOrNull { it.name.startsWith("vAny_") && File(it, "NuGet.Config").exists() }

        val registryKeys get() = registrySubkeys("HKCU\\Software\\JetBrains\\$platform").filter { owns(it.substringAfterLast('\\')) }
        val commandLineKeys get() = registrySubkeys("HKCU\\Software\\Microsoft\\VisualStudio\\${vs.majorVersion}.0_${vs.instanceId}\\AppCommandLine")
            .filter { key -> key.substringAfterLast('\\').let { it.startsWith("ReSharper.") && it.endsWith(".$suffix", ignoreCase = true) } }
    }

    /** Why the instance needs setting up, or null if it's ready. */
    private fun setupProblem(instance: Instance): String? {
        val currentHost = if (instance.userFile.exists()) Regex("<HostFullIdentifier>([^<]*)</HostFullIdentifier>")
            .find(instance.userFile.readText())?.groupValues?.get(1)?.trim() else null
        return when {
            currentHost == null -> "not set up (no ${instance.userFile})"
            currentHost != instance.hostId -> "set up for $currentHost, not ${instance.vs.displayName} (${instance.hostId})"
            instance.hive == null -> "ReSharper isn't installed in ${instance.hostId}"
            !instance.pluginPackage.exists() -> "${instance.pluginPackage} is missing (an earlier setup didn't finish)"
            else -> null
        }
    }

    private class Step(val description: String, val action: () -> Unit)

    /** What --clean removes, in order: the instance's own uninstaller first, then whatever it leaves behind. */
    private fun cleanSteps(instance: Instance, includeInstallers: Boolean): List<Step> = buildList {
        fun delete(file: File, what: String) {
            if (file.exists()) add(Step("delete $what: $file") { file.deleteRecursively() })
        }
        if (instance.uninstaller.exists()) add(Step("uninstall ReSharper from ${instance.hostId} (${instance.uninstaller.name} /HostsToRemove)") {
            exec(instance.uninstaller.path, "/HostsToRemove=${instance.hostId}", "/Silent=True")
        })
        delete(instance.installFolder, "ReSharper installation")
        instance.settingsDirectories.forEach { delete(it, "ReSharper settings") }
        instance.transientDirectories.forEach { delete(it, "ReSharper caches") }
        (instance.registryKeys + instance.commandLineKeys).forEach { key -> add(Step("delete registry key $key") { regDelete(key) }) }
        delete(instance.visualStudioData, "Visual Studio experimental instance data")
        if (instance.visualStudioSettings.exists()) add(Step("delete Visual Studio settings: ${instance.visualStudioSettings}") {
            instance.visualStudioSettings.deleteRecursively()
            instance.visualStudioSettings.parentFile.delete() // only succeeds if nothing else is left in it
        })
        delete(instance.pluginPackage, "installed plugin package")
        delete(instance.userFile, "HostFullIdentifier marker")
        delete(instance.logFile, "ReSharper log")
        // ReSharper rotates the log (ReSharper.1.log), splits out errors (ReSharper.err.log) and its process elevator logs
        // next to it (JetBrains.Process.Elevator.<timestamp>.<pid>.log)
        val logBase = instance.logFile.nameWithoutExtension
        val logExtension = instance.logFile.extension
        instance.logFile.parentFile.listFiles { f ->
            f.isFile && ((f.name.startsWith("$logBase.") && f.name.endsWith(".$logExtension") && f != instance.logFile) ||
                (f.name.startsWith("JetBrains.Process.Elevator.") && f.name.endsWith(".log")))
        }.orEmpty().sortedBy { it.name }.forEach { delete(it, "ReSharper log") }
        delete(instance.packedPlugin, "packed plugin")
        if (includeInstallers) delete(required(installerDirectory, "installerDirectory"), "downloaded installers")
    }

    private fun plan(instance: Instance, cleaning: Boolean, stopAfterClean: Boolean): String = buildString {
        val (release, link) = resolveInstaller(sdkVersion.get())
        val installer = File(required(installerDirectory, "installerDirectory"), link.substringAfterLast('/'))
        val current = setupProblem(instance)
        appendLine("Visual Studio:  ${instance.vs.displayName} ${instance.vs.version} (instance ${instance.vs.instanceId})")
        appendLine("  devenv:       ${instance.vs.devenv}")
        appendLine("SdkVersion:     ${sdkVersion.get()} -> ReSharper $release")
        appendLine("  installer:    $link")
        appendLine("                " + if (installer.exists()) "cached at $installer" else "not cached; would download to $installer")
        appendLine("Instance:       ${instance.hostId} (/rootSuffix ${instance.suffix}), " +
            if (instance.hive != null) "ReSharper installed" else "ReSharper not installed")
        appendLine("Plugin:         ${instance.id} ${instance.version}, " + (current ?: "set up"))
        appendLine()
        if (cleaning) {
            val steps = cleanSteps(instance, cleanInstallers.get())
            appendLine("Would remove:")
            if (steps.isEmpty()) appendLine("  - (nothing; already clean)")
            steps.forEach { appendLine("  - ${it.description}") }
            if (stopAfterClean) return@buildString
            appendLine()
        }
        appendLine("Would run:")
        if (cleaning || current != null) {
            appendLine("  - install ReSharper into ${instance.hostId} (${installer.name})")
            appendLine("  - register ${instance.id} ${instance.version} in its packages.config; write HostFullIdentifier to ${instance.userFile}")
            appendLine("  - dotnet msbuild Restore;Rebuild;Pack -> ${instance.packedPlugin.parentFile}, unpack into ${instance.pluginPackage.parentFile}")
            appendLine("  - run the installer again so the instance picks the plugin up")
        }
        appendLine("  - dotnet msbuild Restore;Rebuild ${instance.projectFile} (CopyPlugin deploys into the instance)")
        appendLine("  - devenv /rootSuffix ${instance.suffix} /ReSharper.Internal /ReSharper.LogFile ${instance.logFile} /ReSharper.LogLevel Trace")
    }

    private fun install(instance: Instance) {
        val (release, link) = resolveInstaller(sdkVersion.get())
        println("ReSharper $release for SdkVersion ${sdkVersion.get()}")
        val installer = File(required(installerDirectory, "installerDirectory"), link.substringAfterLast('/'))
        if (!installer.exists()) {
            println("Downloading ${installer.name} (several GB)")
            download(link, installer)
        } else {
            println("Using cached installer from $installer")
        }

        println("Installing ReSharper into ${instance.hostId}")
        runInstaller(installer, instance)
        val hive = instance.hive
            ?: throw GradleException("ReSharper didn't install into ${instance.hostId} (no NuGet.Config under %LOCALAPPDATA%\\JetBrains\\${instance.platform})")
        println("Found installation directory at $hive")

        // Register the plugin in the instance's packages.config
        val packagesConfig = File(hive, "packages.config")
        val packages = if (packagesConfig.exists()) parseXml(packagesConfig)
                       else parseXml("""<?xml version="1.0" encoding="utf-8"?><packages></packages>""")
        val entries = packages.getElementsByTagName("package").let { list -> (0 until list.length).map { list.item(it) as Element } }
        if (entries.none { it.getAttribute("id") == instance.id }) {
            val node = packages.createElement("package")
            node.setAttribute("id", instance.id)
            node.setAttribute("version", instance.version)
            packages.documentElement.appendChild(node)
            saveXml(packages, packagesConfig)
        }

        // Point dotnet builds at the instance: CopyPlugin copies the built assembly there on every build
        instance.userFile.writeText(
            "<Project><PropertyGroup Condition=\"'\$(MSBuildRuntimeType)' == 'Core'\">" +
                "<HostFullIdentifier>${instance.hostId}</HostFullIdentifier></PropertyGroup></Project>")

        // Pack the plugin and unpack it into the local plugin repository (as `nuget install` would)
        instance.pluginPackage.deleteRecursively()
        exec("dotnet", "msbuild", "/t:Restore;Rebuild;Pack", instance.projectFile.path, "/v:minimal",
            "/p:Configuration=${configuration.get()}", "/p:PackageVersion=${instance.version}",
            "/p:PackageOutputPath=${instance.packedPlugin.parentFile.path}")
        unpackPackage(instance.packedPlugin, instance.pluginPackage)

        println("Re-running the installer so ${instance.hostId} picks the plugin up")
        runInstaller(installer, instance)
    }

    private fun runInstaller(installer: File, instance: Instance) =
        exec(installer.path, "/VsVersion=${instance.vs.majorVersion}.0", "/SpecificProductNames=ReSharper",
            "/Hive=${instance.suffix}", "/Silent=True")

    /** `nuget install` layout (packages.config style): the .nupkg itself plus its files, minus the packaging metadata. */
    private fun unpackPackage(nupkg: File, destination: File) {
        if (!nupkg.exists()) throw GradleException("Expected the packed plugin at $nupkg")
        destination.mkdirs()
        Files.copy(nupkg.toPath(), File(destination, nupkg.name).toPath(), StandardCopyOption.REPLACE_EXISTING)
        ZipFile(nupkg).use { zip ->
            zip.entries().asSequence()
                .filter { !it.isDirectory }
                .map { it to URLDecoder.decode(it.name.replace("+", "%2B"), Charsets.UTF_8) }
                .filterNot { (_, name) ->
                    name == "[Content_Types].xml" || name.startsWith("_rels/") || name.startsWith("package/") ||
                        (!name.contains('/') && name.endsWith(".nuspec"))
                }
                .forEach { (entry, name) ->
                    val target = File(destination, name)
                    target.parentFile.mkdirs()
                    zip.getInputStream(entry).use { input -> target.outputStream().use { input.copyTo(it) } }
                }
        }
    }

    /** The installer and uninstaller can't change an instance that's open. */
    private fun ensureNotRunning(suffix: String) {
        // Single quotes only: embedded double quotes don't survive Windows argument quoting
        val commandLines = capture("powershell", "-NoProfile", "-NonInteractive", "-Command",
            "Get-CimInstance Win32_Process | Where-Object { \$_.Name -eq 'devenv.exe' } | ForEach-Object { \$_.CommandLine }")
        val pattern = Regex("""/rootSuffix\s+"?${Regex.escape(suffix)}"?(\s|$)""", RegexOption.IGNORE_CASE)
        if (commandLines.lines().any { pattern.containsMatchIn(it) })
            throw GradleException("Close the experimental Visual Studio (/rootSuffix $suffix) first.")
    }

    // --- Visual Studio discovery ---------------------------------------------------------------------------------------

    private class VisualStudio(
        val displayName: String, val version: String, val instanceId: String, val channelId: String,
        val installationPath: File,
    ) {
        val majorVersion get() = version.substringBefore('.')
        val devenv get() = installationPath.listFiles().orEmpty().map { File(it, "IDE/devenv.exe") }.firstOrNull { it.exists() }
            ?: throw GradleException("No devenv.exe under $installationPath")
    }

    /** The newest complete Release-channel instance. */
    private fun findVisualStudio(): VisualStudio {
        val vswhere = File(System.getenv("ProgramFiles(x86)") ?: "C:/Program Files (x86)", "Microsoft Visual Studio/Installer/vswhere.exe")
        if (!vswhere.exists()) throw GradleException("$vswhere not found; is Visual Studio installed?")
        val instances = parseXml(capture(vswhere.path, "-format", "xml", "-products", "*"))
            .getElementsByTagName("instance").let { list -> (0 until list.length).map { list.item(it) as Element } }
            .map { VisualStudio(it.text("displayName"), it.text("installationVersion"), it.text("instanceId"),
                                it.text("channelId"), File(it.text("installationPath"))) }
            .filter { it.channelId.contains("Release") }
        return instances.maxWithOrNull(compareBy<VisualStudio, List<Int>>(versionOrder) { it.version.split('.').map { part -> part.toIntOrNull() ?: 0 } })
            ?: throw GradleException("No complete Release-channel Visual Studio found by $vswhere." + incompleteInstances(vswhere))
    }

    /** vswhere hides instances that are still installing/updating; say so when that's why none were found. */
    private fun incompleteInstances(vswhere: File): String {
        val all = parseXml(capture(vswhere.path, "-all", "-format", "xml", "-products", "*")).getElementsByTagName("instance")
        val names = (0 until all.length).map { all.item(it) as Element }.map { "${it.text("displayName")} ${it.text("installationVersion")}" }
        return if (names.isEmpty()) "" else " Incomplete (still installing or updating?): ${names.joinToString()}."
    }

    // --- ReSharper installer -------------------------------------------------------------------------------------------

    /** The release matching the SDK version (else the newest of its line, with a warning) and its Checked installer link. */
    private fun resolveInstaller(sdkVersion: String): Pair<String, String> {
        val line = sdkVersion.split('.').take(2).joinToString(".")
        val url = "https://data.services.jetbrains.com/products/releases?code=RSU&type=eap&type=release&majorVersion=$line"
        @Suppress("UNCHECKED_CAST")
        val entries = ((JsonSlurper().parseText(fetch(url)) as Map<String, Any?>)["RSU"] as? List<Map<String, Any?>>).orEmpty()
        if (entries.isEmpty()) throw GradleException("No ReSharper $line releases found at $url")
        // The release API names prereleases differently from NuGet: 2026.3.0-eap02 -> 2026.3.EAP2 (releases are identical)
        val apiVersion = Regex("""^(\d+\.\d+)\.0-(eap|rc)0*(\d+)$""", RegexOption.IGNORE_CASE).replace(sdkVersion, "$1.$2$3")
        val entry = entries.firstOrNull { (it["version"] as? String).equals(apiVersion, ignoreCase = true) }
            ?: entries.first().also { logger.warn("No ReSharper release matches SDK version $sdkVersion exactly; using ${it["version"]} (${it["type"]})") }

        @Suppress("UNCHECKED_CAST")
        val link = (((entry["downloads"] as? Map<String, Any?>)?.get("windows") as? Map<String, Any?>)?.get("link") as? String)
            ?: throw GradleException("No Windows download for ReSharper ${entry["version"]}")
        return entry["version"].toString() to link.replace(".exe", ".Checked.exe")
    }

    private fun download(url: String, target: File) {
        target.parentFile.mkdirs()
        val partial = File(target.path + ".part")
        val response = http.send(HttpRequest.newBuilder(URI.create(url)).build(), HttpResponse.BodyHandlers.ofFile(partial.toPath()))
        if (response.statusCode() != 200) throw GradleException("Couldn't download $url: HTTP ${response.statusCode()}")
        // Only a complete download becomes the cached installer
        Files.move(partial.toPath(), target.toPath(), StandardCopyOption.REPLACE_EXISTING)
    }

    private fun fetch(url: String): String {
        val response = http.send(HttpRequest.newBuilder(URI.create(url)).timeout(Duration.ofSeconds(60)).build(),
            HttpResponse.BodyHandlers.ofString())
        if (response.statusCode() != 200) throw GradleException("Couldn't fetch $url: HTTP ${response.statusCode()}")
        return response.body()
    }

    // --- Helpers -------------------------------------------------------------------------------------------------------

    private fun exec(vararg command: String) {
        println("> ${command.joinToString(" ")}")
        execOperations.exec { commandLine(*command) }
    }

    private fun capture(vararg command: String): String {
        val output = ByteArrayOutputStream()
        execOperations.exec { commandLine(*command); standardOutput = output }
        return output.toString(Charsets.UTF_8)
    }

    /** Direct subkeys of a registry key (none if the key doesn't exist). */
    private fun registrySubkeys(key: String): List<String> {
        val output = ByteArrayOutputStream()
        val result = execOperations.exec {
            commandLine("reg", "query", key)
            standardOutput = output
            errorOutput = ByteArrayOutputStream()
            isIgnoreExitValue = true
        }
        if (result.exitValue != 0) return emptyList()
        val prefix = key.replaceFirst("HKCU\\", "HKEY_CURRENT_USER\\") + "\\"
        return output.toString(Charsets.UTF_8).lines().map { it.trim() }
            .filter { it.startsWith(prefix, ignoreCase = true) && !it.substring(prefix.length).contains('\\') }
            .map { "HKCU\\" + it.substringAfter("HKEY_CURRENT_USER\\") }
    }

    private fun regDelete(key: String) {
        execOperations.exec {
            commandLine("reg", "delete", key, "/f")
            standardOutput = ByteArrayOutputStream()
            isIgnoreExitValue = true
        }
    }

    private fun required(property: RegularFileProperty, what: String): File =
        property.orNull?.asFile ?: throw GradleException("$name: set $what in the build script")

    private fun required(property: DirectoryProperty, what: String): File =
        property.orNull?.asFile ?: throw GradleException("$name: set $what in the build script")

    private fun localAppData() = File(System.getenv("LOCALAPPDATA") ?: throw GradleException("LOCALAPPDATA isn't set"))

    private fun Element.text(tag: String) = getElementsByTagName(tag).item(0)?.textContent?.trim().orEmpty()

    private fun parseXml(file: File): Document = DocumentBuilderFactory.newInstance().newDocumentBuilder().parse(file)
    private fun parseXml(text: String): Document =
        DocumentBuilderFactory.newInstance().newDocumentBuilder().parse(text.byteInputStream())

    private fun saveXml(document: Document, file: File) {
        TransformerFactory.newInstance().newTransformer().apply {
            setOutputProperty(OutputKeys.INDENT, "yes")
            setOutputProperty(OutputKeys.ENCODING, "utf-8")
        }.transform(DOMSource(document), StreamResult(file))
    }

    companion object {
        val USAGE = """
            |Usage: ./gradlew runVisualStudio [options]
            |
            |Runs the plugin's ReSharper build in an experimental Visual Studio instance (devenv /rootSuffix). The first run
            |sets the instance up: downloads the ReSharper installer for the configured SDK version, installs ReSharper into
            |the instance and installs the plugin package. Later runs rebuild the plugin (the build copies it into the
            |instance) and launch Visual Studio with ReSharper's internal mode and trace logging.
            |
            |Options:
            |  --plan                  Show what would happen; change nothing. Combine with --clean/--reinstall to preview them.
            |  --clean                 Remove the experimental instance: uninstall ReSharper from it, then delete its settings,
            |                          caches, registry keys, Visual Studio data and installed plugin package, plus the
            |                          .csproj.user marker, the log and the packed plugin. Keeps the downloaded installers.
            |                          Never touches the normal (non-suffixed) Visual Studio or ReSharper.
            |  --clean-installers      --clean, and also delete the downloaded ReSharper installers.
            |  --reinstall             --clean, then set the instance up from scratch and launch.
            |  --root-suffix <name>    Experimental instance name (devenv /rootSuffix); overrides the build script's.
            |  --plugin-version <ver>  Version the plugin package is installed as. Default: 9999.0.0.
            |  --usage                 This text.
            |
            |Configured in the build script: pluginId, projectFile, sdkVersion, rootSuffix, installerDirectory,
            |packageOutputDirectory, logFile, and optionally pluginVersion and configuration (Debug).
            |""".trimMargin()

        private val http: HttpClient = HttpClient.newBuilder()
            .followRedirects(HttpClient.Redirect.NORMAL)
            .connectTimeout(Duration.ofSeconds(20))
            .build()

        private val versionOrder = Comparator<List<Int>> { a, b ->
            (0 until maxOf(a.size, b.size)).map { a.getOrElse(it) { 0 }.compareTo(b.getOrElse(it) { 0 }) }.firstOrNull { it != 0 } ?: 0
        }
    }
}
