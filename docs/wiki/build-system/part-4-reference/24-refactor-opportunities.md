# 24 · Refactor opportunities

**[Reference]** — *A backlog of "this is what's NOT ideal and what to do about it." Each item is captured for future maintenance, not committed-to action.*

The build works today. These are improvements that would reduce surface area, improve incrementality, eliminate drift, or close gaps that bite during upgrades. Each is tied to a specific maintainer goal or a §17 quirk.

## 1. Consolidate version pinning into `libs.versions.toml`

**Goal addressed**: "the smaller the surface area I have to worry about, the better"
**Files**: `gradle.properties:24-27` ↔ `gradle/libs.versions.toml`, `build.gradle.kts:9-10`, `settings.gradle.kts:29-34`

Currently:
- `gradle.properties` holds: `rdVersion`, `rdKotlinVersion`, `intellijPlatformGradlePluginVersion`, `gradleJvmWrapperVersion`
- `libs.versions.toml` holds: `kotlin`, `rdGen`
- `build.gradle.kts` re-declares versions inline for IPGP and jvm-wrapper

Target state: every version in `libs.versions.toml`. Plugin versions exposed via `[plugins]` table. `pluginManagement` in `settings.gradle.kts` reads via the `versionCatalogs { ... }` block (Gradle 7.4+ permits this).

Migration:
1. Add to `libs.versions.toml` `[versions]`: `rdGen` (already there), `intellijPlatformGradlePlugin`, `gradleJvmWrapper`, `rdKotlin`
2. Add `[plugins]` entries for all four
3. In `settings.gradle.kts`, replace `String by settings` reads with version-catalog access
4. In `build.gradle.kts`, replace inline `version "..."` with `alias(libs.plugins...)`
5. Delete the corresponding `gradle.properties` lines
6. Test: `./gradlew :buildPlugin` from a clean cache

Tradeoffs: a couple-day refactor; touches initialization-phase code which can be subtle. Worthwhile because it stops the silent-drift problem.

## 2. Delete `riderBaseVersion`

**State**: trivial
**Files**: `gradle.properties:28`

Zero references in the entire codebase. Delete the line.

## 3. Delete legacy PowerShell scripts

**Goal addressed**: "if I can ... rewrite the runVisualStudio.ps1 and buildPlugin.ps1 ... If I'm even using the build one?!" — confirmed: not used by CI
**Files**: `buildPlugin.ps1`, `publishPlugin.ps1`, `settings.ps1`, `tools/vswhere.exe`, `tools/nuget.exe`, `.run/Build ReSharper Plugin.run.xml`

Steps:
1. Delete the four scripts and `tools/` directory
2. Replace `.run/Build ReSharper Plugin.run.xml` with a Gradle run config calling `:buildResharperPlugin`
3. Update `README.md` if it references the scripts (check first)

Decision deferred: `runVisualStudio.ps1` has a real local-dev use case (set up an experimental ReSharper hive in Visual Studio for ReSharper-for-VS development). Decide whether to keep it as-is, port it to Kotlin, or delete it depending on whether you actively support that flow.

## 4. Convert .NET `Exec` tasks to typed task class in `buildSrc/`

**Goal addressed**: "rewrite the runVisualStudio.ps1 and buildPlugin.ps1 (If I'm even using the build one?!) into a Kotlin class that I can plug into Gradle then that'll make the build system that much more digestible"
**Files**: `build.gradle.kts:86-105, :226-230`

Create `buildSrc/src/main/kotlin/DotNetBuildTask.kt`:

```kotlin
import org.gradle.api.DefaultTask
import org.gradle.api.tasks.*
import org.gradle.process.ExecOperations
import javax.inject.Inject

abstract class DotNetBuildTask @Inject constructor(
    private val execOps: ExecOperations
) : DefaultTask() {
    @get:InputDirectory abstract val sourceDir: DirectoryProperty
    @get:OutputDirectory abstract val outputDir: DirectoryProperty
    @get:Input abstract val configuration: Property<String>
    @get:Input abstract val solution: Property<String>

    @TaskAction
    fun run() {
        execOps.exec {
            executable = "dotnet"
            args("build", solution.get(), "--configuration", configuration.get(), "--consoleLoggerParameters:ErrorsOnly")
        }
    }
}
```

Then in `build.gradle.kts`:

```kotlin
val compileDotNet by tasks.registering(DotNetBuildTask::class) {
    sourceDir.set(layout.projectDirectory.dir("src/dotnet"))
    outputDir.set(layout.projectDirectory.dir("src/dotnet/${DotnetPluginId}/bin"))
    configuration.set(BuildConfiguration)
    solution.set(DotnetSolution)
}
```

Result:
- `compileDotNet` is now incremental (skips when no `.cs` changed)
- Configuration cache compatible
- Two more typed tasks for `buildResharperPlugin` and `testDotNet`

This is the user's stated "rewrite into a Kotlin class that I can plug into Gradle" goal.

## 5. Extract DotFiles patcher into its own task

**Files**: `build.gradle.kts:186-222`

Create `buildSrc/src/main/kotlin/PatchSandboxDotFilesTask.kt` extending `DefaultTask` with `@InputFiles` (candidate Rider install paths) and `@OutputFiles` (destination DLLs). `prepareSandbox` finalizes with it.

Result: removes the imperative `doLast` from `prepareSandbox`. The side-effect becomes cacheable.

## 6. Replace the `if (!isWindows)` filesystem walk with a `ValueSource`

**Files**: `build.gradle.kts:188-201`

A `ValueSource` is Gradle's mechanism for "compute a value from the environment, configurable-cache-friendly." Probe candidate Rider install paths inside a `ValueSource` rather than imperatively in `doLast`.

Result: configuration cache works smoothly even with the patcher running.

## 7. Wrap `CHANGELOG.md` parsing in `provider { }`

**Files**: `build.gradle.kts:239-247`

Today the file is read at configuration time (every build, even no-op ones). Move inside `provider { }` for laziness:

```kotlin
tasks.patchPluginXml {
    pluginVersion.set(PluginVersion)
    changeNotes.set(provider {
        val changelogText = file("${rootDir}/CHANGELOG.md").readText()
            .lines()
            .dropWhile { !it.trim().startsWith("##") }
            .drop(1)
            .takeWhile { !it.trim().startsWith("##") }
            .filter { it.trim().isNotEmpty() }
            .joinToString("\r\n") {
                "<li>${it.trim().replace(Regex("^\\*\\s+?"), "")}</li>"
            }.trim()
        "<ul>\r\n$changelogText\r\n</ul>"
    })
    untilBuild.set(provider { null })
}
```

Result: configuration phase faster, no-op builds don't read the file.

## 8. Drop redundant `apply { plugin("kotlin") }`

**Files**: `build.gradle.kts:53-55`

Already covered by `alias(libs.plugins.kotlinJvm)` at line 8. Delete.

## 9. Replace `${buildDir}` with lazy form

**Files**: `build.gradle.kts:110`

```kotlin
val pluginZip = layout.buildDirectory.file("distributions/${rootProject.name}-${version}.zip")
val outputDir = layout.projectDirectory.dir("output").asFile

tasks.buildPlugin {
    doLast {
        val zipFile = pluginZip.get().asFile
        outputDir.mkdirs()
        zipFile.copyTo(outputDir.resolve(zipFile.name), overwrite = true)
    }
}
```

Mandatory before Gradle 10. Otherwise the build will break loudly.

## 10. Reconsider `me.filippov.gradle.jvm.wrapper`

**Files**: `build.gradle.kts:10`, `gradle.properties:27`

Gradle 7.6+ has built-in JVM toolchain auto-provisioning via the `foojay-resolver-convention` plugin. That covers most of what `gradle-jvm-wrapper` provides for fresh-clone bootstrapping.

Steps:
1. Add `foojay-resolver-convention` to `pluginManagement.plugins` in `settings.gradle.kts`
2. Remove `id("me.filippov.gradle.jvm.wrapper")` from `build.gradle.kts:10`
3. Remove `gradleJvmWrapperVersion` from `gradle.properties:27` and `settings.gradle.kts:7`
4. Test on a machine without JDK 21 installed: `./gradlew runIde`

Tradeoff: removes one duplicated version pin and one third-party plugin dep.

## 11. Update `.run/Build Plugin.run.xml` JDK reference

**Files**: `.run/Build Plugin.run.xml:8`

Change `corretto-17.0.7` to `corretto-21`, or remove the `JAVA_HOME` env var entirely (the toolchain handles it).

## 12. Add a fixture-mod-driven `:integrationTest` task

**Goal addressed**: "Including a mod as a fixture should help make it easier to launch and test with new versions"
**Approach**:

1. Promote `example-mod/` to a real fixture mod (or create `tests/fixture-mod/` with a more controlled setup)
2. Add a Gradle `:integrationTest` task that:
   - Depends on `prepareSandbox`
   - Boots Rider headlessly against the fixture
   - Asserts known completion items / references / run-config behavior
3. Register the task in CI's `Test` job

This is a substantial undertaking. JetBrains' `RiderTestBase` infrastructure is undocumented; contact JetBrains via Slack (§23) before starting.

## 13. Fill in `ReSharperPlugin.RimworldDev.Tests`

**Goal addressed**: testability ramp
**Files**: `src/dotnet/ReSharperPlugin.RimworldDev.Tests/`

Steps:
1. Add `<PackageReference Include="JetBrains.ReSharper.SDK.Tests" Version="$(SdkVersion)" />` to the test csproj
2. Add a first ReSharper-style fixture for `RimworldXMLItemProvider` (using `BaseTestWithSingleProject` or similar)
3. Use `JetBrains/resharper-unity`'s `Unity.Tests.csproj` as the reference

Lower-effort than #12 — pure ReSharper tests don't need to boot a Rider host.

## 14. Add a `verifyDotNetOutputs` task

**Files**: `build.gradle.kts:181-184`

Decouple the existence-check `doLast` from `prepareSandbox`. Create a separate task with proper `@InputFiles`. `prepareSandbox` becomes pure declarative file copying; `verifyDotNetOutputs` runs as part of the chain. Cleaner separation of concerns and gives `prepareSandbox` better incrementality.

## 15. Wire up `verifyPlugin` in CI

**Files**: `.github/workflows/CI.yml`

The `verifyPlugin` task isn't currently run by CI. Add a step:

```yaml
- run: ./gradlew :verifyPlugin --no-daemon
```

Result: catch binary-incompat issues across Rider versions before they ship.

## Prioritization (a suggestion, not a mandate)

| Priority | Item | Rationale |
|---|---|---|
| High | #2, #8, #11 | Trivial cleanups, immediate signal-to-noise improvement |
| High | #1 | The user's stated "consolidate versions" goal; affects all future bumps |
| High | #9 | Mandatory before Gradle 10 |
| Medium | #3 | The user's "PowerShell" question; cleaner repo |
| Medium | #4 | Improves incrementality and config cache; user's stated "Kotlin class" goal |
| Medium | #15 | Catches platform-incompat issues earlier |
| Lower | #5, #6, #7 | Polish; unblocks config cache improvements |
| Lower | #10 | Removes a dependency; not urgent |
| Big effort | #12, #13 | The user's testability goal; needs JetBrains help (§23) |
| Polish | #14 | Cleaner code; not strictly necessary |

→ End of wiki.
