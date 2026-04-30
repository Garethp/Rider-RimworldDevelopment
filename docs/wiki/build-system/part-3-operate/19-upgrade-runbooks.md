# 19 · Upgrade runbooks

**[Operate / Recipes]** — *Precise edit lists for each common upgrade. Cross-reference §07 (version map) and §17 (known issues).*

Each runbook lists the files to edit, the order to do it in, and the verification step. Where two files are listed, edit BOTH unless the version is consolidated (which it isn't yet — see §24).

## Gradle wrapper bump

E.g. 9.4.1 → 10.0.

1. Edit `gradle/wrapper/gradle-wrapper.properties:3`:
   ```properties
   distributionUrl=https\://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-10.0-all.zip
   ```
2. Edit `build.gradle.kts:58`:
   ```kotlin
   gradleVersion = "10.0"
   ```
3. Run the canonical Gradle wrapper update sequence (twice — this is a real Gradle quirk):
   ```bash
   ./gradlew wrapper
   ./gradlew wrapper
   ```
4. Verify with deprecations enabled:
   ```bash
   ./gradlew :buildPlugin --warning-mode all --configuration-cache --build-cache
   ```
5. **Mandatory for Gradle 10**: replace `${buildDir}` (deprecated in 9, removed in 10):
   - `build.gradle.kts:110` — `from("${buildDir}/distributions/...")` → `layout.buildDirectory.file("distributions/...")`
6. Compatibility matrix: <https://docs.gradle.org/current/userguide/compatibility.html>

Reference compatibility:
- Gradle 9.x supports JDK 17, 21
- Gradle 10.x supports JDK 21+
- Kotlin Gradle plugin must be compatible — consult its release notes

## IntelliJ Platform Gradle Plugin (IPGP) bump

E.g. 2.14.0 → 2.15.0.

1. Edit `gradle.properties:26`:
   ```properties
   intellijPlatformGradlePluginVersion=2.15.0
   ```
2. Edit `build.gradle.kts:9`:
   ```kotlin
   id("org.jetbrains.intellij.platform") version "2.15.0"
   ```
3. The two locations must agree (until §24's consolidation refactor lands).
4. Verify:
   ```bash
   ./gradlew :buildPlugin :verifyPlugin
   ```
5. Watch for changes to:
   - The `intellijPlatform { ... }` extension shape (esp. `pluginVerification.ides` API)
   - Renamed `buildFeature` flags (`useBinaryReleases` is one such flag — see §17)
   - `bundledPlugin` vs `bundledModule` reclassifications

Changelog: <https://github.com/JetBrains/intellij-platform-gradle-plugin/releases>

## rdgen / Rider SDK bump

E.g. Rider 2026.1 → 2026.2. This is the *biggest* upgrade because it touches both Gradle and .NET.

1. Edit `gradle.properties:17`:
   ```properties
   ProductVersion=2026.2
   ```
2. Edit `gradle.properties:24`:
   ```properties
   rdVersion=2026.2
   ```
3. Edit `Directory.Build.props:4`:
   ```xml
   <SdkVersion>2026.2.*</SdkVersion>
   ```
4. Edit `gradle/libs.versions.toml:3` to the corresponding `rd-gen` patch:
   ```toml
   rdGen = "2026.2.X"  # see https://github.com/JetBrains/rd/releases for the exact patch
   ```
5. **Verify the Kotlin pin** (`gradle/libs.versions.toml:2 kotlin` and `gradle.properties:25 rdKotlinVersion`) against the Kotlin compatibility matrix:
   <https://plugins.jetbrains.com/docs/intellij/using-kotlin.html#kotlin-standard-library>
   If the bundled Kotlin in the new Rider differs, bump these too.
6. Run rdgen to regenerate bindings (in case the Rider model changed):
   ```bash
   ./gradlew :protocol:rdgen
   ```
7. Check that the regenerated `*.Generated.kt` and `*.Generated.cs` still compile against your hand-written code:
   ```bash
   ./gradlew :buildPlugin
   ./gradlew compileDotNet
   ```
8. Test in a sandbox:
   ```bash
   ./gradlew runIde
   ```
9. Update `plugin.xml:6 since-build` to the matching IDE build number if you want to formally signal compatibility (the build number for 2026.2 would be `262`).

Reference compatibility:
- `rdGen` major.minor typically matches `Rider` major.minor
- `rdGen` patch can be bumped independently for bug fixes
- `JetBrains.RdFramework` and `JetBrains.Lifetimes` (in `Directory.Build.props`) are pinned to `$(SdkVersion)` and follow

Changelog: <https://github.com/JetBrains/rd/releases>

## Kotlin bump

E.g. 2.3.20 → 2.4.0.

1. Edit `gradle/libs.versions.toml:2`:
   ```toml
   kotlin = "2.4.0"
   ```
2. Edit `gradle.properties:25` (the version used inside `pluginManagement`):
   ```properties
   rdKotlinVersion=2.4.0
   ```
3. Verify against the Kotlin ↔ IntelliJ Platform matrix:
   <https://plugins.jetbrains.com/docs/intellij/using-kotlin.html#kotlin-standard-library>
   The bundled Kotlin in the IDE must support compiling against your version.
4. Test:
   ```bash
   ./gradlew :buildPlugin
   ```

## JDK bump

E.g. 21 → 22.

Five places, all need to agree:

1. `build.gradle.kts:14-20` — toolchain block (`JavaLanguageVersion.of(22)`)
2. `build.gradle.kts:82-84` — Kotlin's `JvmTarget` (`JvmTarget.JVM_22`)
3. `.github/workflows/CI.yml:21` — `java-version: '22'`
4. `.github/workflows/Deploy.yml:21` — `java-version: '22'`
5. `.run/Build Plugin.run.xml:8` — JDK reference (currently stale at `corretto-17.0.7` — fix to 21 first as part of any toolchain work)

Compatibility:
- The IntelliJ Platform itself ships built for a specific JDK. Don't go above what the platform supports — consult the IPGP changelog and Rider release notes.
- Gradle and Kotlin Gradle Plugin must support the JDK; consult <https://docs.gradle.org/current/userguide/compatibility.html>.

## .NET SDK bump

E.g. 7.0.202 → 8.0.x.

1. Edit `global.json:3-5`:
   ```json
   {
     "sdk": {
       "version": "8.0.100",
       "rollForward": "latestMajor",
       "allowPrerelease": true
     }
   }
   ```
2. Verify against the Rider host TFM. The plugin currently targets `net6.0` (in both csprojs at line 11). Bumping the SDK is generally backwards-compatible with old TFMs; bumping the *target framework* requires Rider to support it (consult JetBrains).
3. CI's `actions/setup-dotnet` versions are usually broader (`8.0.x`) and don't need updating unless you go major.

## Adding a new bundled-plugin or bundled-module dependency

1. Edit `build.gradle.kts:117-127` `dependencies.intellijPlatform { }` block:
   ```kotlin
   bundledPlugin("com.example.plugin")
   // or
   bundledModule("intellij.example")
   ```
2. Edit `src/rider/main/resources/META-INF/plugin.xml` to declare the runtime dependency:
   ```xml
   <depends>com.example.plugin</depends>
   ```
3. Test: `./gradlew runIde` — verify the launched Rider has the dependency loaded and your plugin loads.

Watch out: bundled plugin vs. bundled module categorization can shift between Platform versions. If `bundledPlugin("...")` errors with "not found," try `bundledModule("...")` and consult the IPGP changelog.

## Common mistakes to avoid during an upgrade

- **Bumping `gradle.properties` but not `build.gradle.kts`** (or vice versa) for IPGP / jvm-wrapper. Both must agree until §24's consolidation refactor.
- **Forgetting to run `:protocol:rdgen` after a Rider SDK bump.** The model is checked in, but it's against the *old* SDK. New SDK may add fields that shift hashes.
- **Not running with `--warning-mode all` after an upgrade.** Deprecations get loud one version before they break. Reading them gives you advance warning.
- **Using `--configuration-cache` only after the upgrade is "done."** Run with it the whole time; cache hostility surfaces as a config-time error during cache write, which is the easiest place to fix it.
- **Ignoring `verifyPlugin` failures.** They catch real binary-incompat issues. Run `./gradlew verifyPlugin` after any IDE-side bump.

## Compatibility-matrix anchor URLs (one-stop lookup)

- **Gradle ↔ JDK ↔ Kotlin Gradle Plugin**: <https://docs.gradle.org/current/userguide/compatibility.html>
- **Kotlin ↔ IntelliJ Platform**: <https://plugins.jetbrains.com/docs/intellij/using-kotlin.html#kotlin-standard-library>
- **rd / rdgen releases**: <https://github.com/JetBrains/rd/releases>
- **IPGP releases / changelog**: <https://github.com/JetBrains/intellij-platform-gradle-plugin/releases>
- **Rider build numbers ↔ release names**: <https://plugins.jetbrains.com/docs/intellij/intellij-platform-versions.html>
- **JetBrains Plugin Verifier**: <https://plugins.jetbrains.com/docs/intellij/verifying-plugin-compatibility.html>

→ End of Part 3. Next: [20 · Task graph diagrams](../part-4-reference/20-task-graph-diagrams.md)
