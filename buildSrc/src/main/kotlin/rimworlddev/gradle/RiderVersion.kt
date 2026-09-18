package rimworlddev.gradle

/**
 * A JetBrains.Rider.SDK version as NuGet publishes it: 2026.1.5.2, 2026.2.0-rc01, 2026.3.0-eap02.
 *
 * `<SdkVersion>` in Directory.Build.props is written in this form; the companion holds the mappings to the other
 * version formats derived from it (the IntelliJ Platform's Maven version, ReSharper's wave).
 */
data class RiderVersion(val raw: String, val numbers: List<Int>, val preKind: String?, val preNumber: Int) :
    Comparable<RiderVersion> {

    /** "2026.1" */
    val line get() = "${numbers[0]}.${numbers[1]}"
    val stable get() = preKind == null

    override fun compareTo(other: RiderVersion): Int {
        for (i in 0 until maxOf(numbers.size, other.numbers.size)) {
            val c = numbers.getOrElse(i) { 0 }.compareTo(other.numbers.getOrElse(i) { 0 })
            if (c != 0) return c
        }
        // A release sorts after its prereleases; an RC after the EAPs.
        fun rank(v: RiderVersion) = when (v.preKind) { null -> 2; "rc" -> 1; else -> 0 }
        return compareValuesBy(this, other, { rank(it) }, { it.preNumber })
    }

    companion object {
        private val pattern = Regex("""^(\d{4}(?:\.\d+){1,3})(?:-(eap|rc)(\d+))?$""", RegexOption.IGNORE_CASE)

        /** `<SdkVersion>…</SdkVersion>` in Directory.Build.props; group 1 is the version. */
        val sdkVersionPattern = Regex("""<SdkVersion>\s*([^<\s]+)\s*</SdkVersion>""")

        fun parse(raw: String) = pattern.matchEntire(raw)?.let { m ->
            RiderVersion(raw, m.groupValues[1].split('.').map(String::toInt),
                m.groupValues[2].lowercase().ifEmpty { null }, m.groupValues[3].toIntOrNull() ?: 0)
        }

        /** "2026.1" -> sortable number, so 2026.10 would sort after 2026.9. */
        fun lineKey(line: String) = line.split('.').let { it[0].toInt() * 1000 + it[1].toInt() }

        /**
         * Rider's Maven artifacts (what the IntelliJ Platform Gradle Plugin downloads) name the same builds differently
         * from NuGet: 2026.3.0-eap02 -> 2026.3-EAP2-SNAPSHOT, 2026.2.0-rc01 -> 2026.2-RC1-SNAPSHOT, 2026.2.0 -> 2026.2,
         * 2026.1.5.2 -> 2026.1.5.2.
         */
        fun mavenVersion(sdkVersion: String): String {
            Regex("""^(\d+\.\d+)\.0-(eap|rc)0*(\d+)$""", RegexOption.IGNORE_CASE).matchEntire(sdkVersion)?.let { m ->
                return "${m.groupValues[1]}-${m.groupValues[2].uppercase()}${m.groupValues[3]}-SNAPSHOT"
            }
            Regex("""^(\d+\.\d+)\.0$""").matchEntire(sdkVersion)?.let { return it.groupValues[1] }
            return sdkVersion
        }

        /** Same derivation as WaveVersionBase in Directory.Build.props: 2026.3.0-eap02 -> 263. */
        fun waveBase(sdkVersion: String) = Regex("""^\d\d(\d\d)\.(\d+)\..*$""").replace(sdkVersion, "$1$2")
    }
}
