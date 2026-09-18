package rimworlddev.gradle

import org.gradle.api.DefaultTask
import org.gradle.api.GradleException
import org.gradle.api.file.RegularFileProperty
import org.gradle.api.provider.Property
import org.gradle.api.tasks.Input
import org.gradle.api.tasks.Internal
import org.gradle.api.tasks.Optional
import org.gradle.api.tasks.TaskAction
import org.gradle.api.tasks.options.Option
import java.net.URI
import java.net.http.HttpClient
import java.net.http.HttpRequest
import java.net.http.HttpResponse
import java.time.Duration

/**
 * `./gradlew versions` lists every build of the current Rider EAP cycle (if there is one) and the latest release of the
 * last three stable lines, marking the one this checkout targets. `./gradlew versions --to <target>` switches by
 * rewriting `<SdkVersion>` in Directory.Build.props; `./gradlew versions --usage` lists the accepted targets
 * (Gradle keeps `--help` for itself).
 *
 * Versions come from NuGet's JetBrains.Rider.SDK index (what the backend restores). Each is also checked against the
 * JetBrains Maven repository the Gradle side downloads Rider from, and against the Wave package, which can both lag NuGet
 * (and Maven drops old EAP snapshots). Everything happens at execution time, so the configuration cache is unaffected.
 */
abstract class RiderVersionsTask : DefaultTask() {
    @get:Internal
    abstract val propsFile: RegularFileProperty

    @get:Input
    @get:Optional
    @get:Option(option = "to", description = "Switch to a Rider version: eap, eap3, 2026.2-eap5, latest, 2026.1 or an exact version. See --usage.")
    abstract val to: Property<String>

    @get:Input
    @get:Optional
    @get:Option(option = "usage", description = "Print what --to accepts, with examples.")
    abstract val usage: Property<Boolean>

    @TaskAction
    fun run() {
        if (usage.getOrElse(false)) {
            print(USAGE)
            return
        }

        val file = propsFile.get().asFile
        val props = file.readText()
        val current = RiderVersion.sdkVersionPattern.find(props)?.groupValues?.get(1)
            ?: throw GradleException("No <SdkVersion> found in $file")

        val all = fetch(NUGET_INDEX).let { json ->
            Regex(""""versions"\s*:\s*\[(.*?)]""", RegexOption.DOT_MATCHES_ALL).find(json)?.groupValues?.get(1)
                ?.let { list -> Regex(""""([^"]+)"""").findAll(list).map { it.groupValues[1] }.toList() }
                ?: throw GradleException("Unexpected response from $NUGET_INDEX")
        }.mapNotNull(RiderVersion::parse)
        val lines = all.groupBy { it.line }.toSortedMap(compareByDescending<String> { RiderVersion.lineKey(it) })
        // The EAP cycle in progress: the newest line, if it has no stable release yet. All its builds, newest first.
        val eaps = lines.entries.firstOrNull()?.value?.takeIf { versions -> versions.none { it.stable } }
            ?.sortedDescending().orEmpty()
        val stable = lines.values.mapNotNull { versions -> versions.filter { it.stable }.maxOrNull() }.take(3)

        var target = current
        to.orNull?.trim()?.let { requested ->
            fun fail(message: String): Nothing =
                throw GradleException("$message Run `./gradlew versions --usage` for what --to accepts. Available:\n" +
                    listing(eaps, stable, current))

            val build = Regex("""^(?:(\d{4}\.\d+)-)?(eap|rc)0*(\d+)$""", RegexOption.IGNORE_CASE).matchEntire(requested)
            target = when {
                requested.equals("eap", ignoreCase = true) ->
                    eaps.firstOrNull()?.raw ?: fail("There is no Rider EAP at the moment.")
                build != null -> {
                    val (line, kind, number) = build.destructured
                    val cycle = if (line.isEmpty()) eaps.ifEmpty { fail("There is no Rider EAP at the moment; name the line, e.g. 2026.2-$kind$number.") }
                                else lines[line] ?: fail("No Rider $line versions found.")
                    cycle.firstOrNull { it.preKind == kind.lowercase() && it.preNumber == number.toInt() }?.raw
                        ?: fail("No ${kind.uppercase()}$number in Rider ${line.ifEmpty { eaps.first().line }}.")
                }
                requested.equals("latest", ignoreCase = true) -> stable.first().raw
                Regex("""^\d{4}\.\d+$""").matches(requested) -> lines[requested]
                    ?.let { versions -> (versions.filter { it.stable }.maxOrNull() ?: versions.max()).raw }
                    ?: fail("No Rider $requested versions found.")
                else -> all.firstOrNull { it.raw.equals(requested, ignoreCase = true) }?.raw
                    ?: fail("$requested isn't a published JetBrains.Rider.SDK version.")
            }
            if (target == current) {
                println("Already on $current.")
            } else {
                file.writeText(RiderVersion.sdkVersionPattern.replace(props) { "<SdkVersion>$target</SdkVersion>" })
                println("SdkVersion: $current -> $target (Directory.Build.props)")
                println("Rider reloads the solution by itself; the next Gradle run builds against ${RiderVersion.mavenVersion(target)}.")
            }
            println()
        }

        print(listing(eaps, stable, target))
    }

    private fun listing(eaps: List<RiderVersion>, stable: List<RiderVersion>, current: String): String {
        val maven = listOf(MAVEN_RELEASES, MAVEN_SNAPSHOTS).flatMapTo(mutableSetOf()) { url ->
            runCatching { Regex("<version>([^<]+)</version>").findAll(fetch(url)).map { it.groupValues[1] }.toList() }
                .getOrDefault(emptyList())
        }
        val waves = runCatching { Regex(""""([^"]+)"""").findAll(fetch(WAVE_INDEX)).map { it.groupValues[1] }.toSet() }
            .getOrDefault(emptySet())

        fun row(kind: String, version: String): String {
            val product = RiderVersion.mavenVersion(version)
            val wave = "${RiderVersion.waveBase(version)}.0.0"
            val notes = buildList {
                if (maven.isNotEmpty() && product !in maven) add("not on JetBrains Maven")
                if (waves.isNotEmpty() && wave !in waves) add("no Wave $wave yet")
                if (version == current) add("<- current")
            }
            return "  %-7s %-17s %-22s %s".format(kind, version, product, notes.joinToString(", ")).trimEnd() + "\n"
        }

        return buildString {
            append("Rider versions (NuGet JetBrains.Rider.SDK / IntelliJ Platform):\n")
            if (eaps.isEmpty()) append("  EAP     none at the moment\n")
            eaps.forEach { append(row(it.preKind!!.uppercase(), it.raw)) }
            stable.forEach { append(row("Stable", it.raw)) }
            if (eaps.none { it.raw == current } && stable.none { it.raw == current }) append(row("Current", current))
        }
    }

    companion object {
        val USAGE = """
            |Usage:
            |  ./gradlew versions                  List every build of the current Rider EAP and the latest release of the
            |                                      last three stable lines, marking the one this checkout targets.
            |  ./gradlew versions --to <target>    Switch by rewriting <SdkVersion> in Directory.Build.props.
            |  ./gradlew versions --usage          This text.
            |
            |<target>:
            |  eap                  newest build of the current EAP cycle
            |  eap3, eap03, rc1     that build of the current EAP cycle (e.g. to find which EAP broke something)
            |  2026.2-eap5          that build of another line's EAP cycle
            |  latest               newest stable release
            |  2026.1               newest release of that line (its newest EAP/RC if it has no release yet)
            |  2026.2.0-rc01        an exact JetBrains.Rider.SDK version, as NuGet publishes it
            |
            |Run it on its own: Rider reloads the solution by itself and the next Gradle run builds against the new
            |version. "not on JetBrains Maven" means the Gradle side can't download that Rider build (Maven lags NuGet,
            |and drops old EAP snapshots); the backend still restores and builds against it.
            |""".trimMargin()

        private const val NUGET_INDEX = "https://api.nuget.org/v3-flatcontainer/jetbrains.rider.sdk/index.json"
        private const val WAVE_INDEX = "https://api.nuget.org/v3-flatcontainer/wave/index.json"
        private const val MAVEN_RELEASES =
            "https://cache-redirector.jetbrains.com/intellij-repository/releases/com/jetbrains/intellij/rider/riderRD/maven-metadata.xml"
        private const val MAVEN_SNAPSHOTS =
            "https://cache-redirector.jetbrains.com/intellij-repository/snapshots/com/jetbrains/intellij/rider/riderRD/maven-metadata.xml"

        private val http: HttpClient = HttpClient.newBuilder()
            .followRedirects(HttpClient.Redirect.NORMAL)
            .connectTimeout(Duration.ofSeconds(20))
            .build()

        private fun fetch(url: String): String {
            val request = HttpRequest.newBuilder(URI.create(url)).timeout(Duration.ofSeconds(60)).build()
            val response = runCatching { http.send(request, HttpResponse.BodyHandlers.ofString()) }
                .getOrElse { throw GradleException("Couldn't fetch $url: ${it.message}", it) }
            if (response.statusCode() != 200) throw GradleException("Couldn't fetch $url: HTTP ${response.statusCode()}")
            return response.body()
        }
    }
}
