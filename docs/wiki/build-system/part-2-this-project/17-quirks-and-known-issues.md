# 17 · Quirks and known issues

**[This Project]** — *A ledger of "this looks weird because it IS weird." Each entry is a callout, with file:line and current state.*

Use this page when you encounter something that doesn't quite make sense — there's a good chance it's listed here. Each entry has a **state** tag:

- **intentional** — exists for a real reason, leave it
- **workaround** — works around a JetBrains gap; we live with it
- **stale-delete** — dead, should be removed (refactor backlog in §24)
- **drift** — same thing pinned in two places, gradually diverging
- **needs-jetbrains** — root cause is upstream; track via a JetBrains ticket

## DotFiles patcher (Mac/Linux only)

**State**: `needs-jetbrains` + `workaround`
**File**: `build.gradle.kts:186-222`

Copies `JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.PausePoint.Helper.dll` and similar from a local Rider install into the sandbox's SDK directory because the cross-platform Maven artifact strips them. Silently no-ops if no Rider is installed locally — that's the silent footgun. Full discussion in §14.

The right fix is upstream: either ship those DLLs in the Maven artifact, or let IPGP fetch them on demand. Should have a YouTrack RIDER ticket linked from this section. Until that's resolved, the wiki marks this as **Help wanted from JetBrains** (§23).

## Hand-rolled Remodder DLL list

**State**: `workaround`
**File**: `build.gradle.kts:160-171`

Eight DLL filenames hard-coded into the `prepareSandbox` configuration. If a new NuGet runtime dependency is added to `.Rider.csproj`, you must also add its DLL filename here. The build doesn't enforce this — the failure manifests at plugin load time as a `FileNotFoundException`.

Refactor candidate: derive the list automatically by globbing `bin/.../*.dll` (or by reading the .csproj's resolved transitive closure). §24.

## `compileDotNet` etc. have no incrementality

**State**: `workaround`
**Files**: `build.gradle.kts:86-90, :92-105, :226-230`

The `Exec`-based .NET tasks (`compileDotNet`, `buildResharperPlugin`, `testDotNet`) declare no `@InputDirectory` / `@OutputDirectory`. Gradle has no idea what's an input or output. Consequences:

- They re-run on every build, even when no `.cs` file changed
- They're incompatible with Gradle's build cache
- Configuration cache support is degraded

`dependsOn(compileDotNet)` orders dependent tasks correctly but doesn't make `compileDotNet` skippable. Refactor candidate: convert to a typed task class in `buildSrc/` with proper inputs/outputs declared. §24.

## `apply { plugin("kotlin") }` is redundant

**State**: `stale-delete`
**File**: `build.gradle.kts:53-55`

The Kotlin JVM plugin was already applied via `alias(libs.plugins.kotlinJvm)` at line 8. Dead code from a template ancestor. Safe to delete.

## `riderBaseVersion=2025.1`

**State**: `stale-delete`
**File**: `gradle.properties:28`

Zero references in the entire codebase (Kotlin, Gradle, PowerShell, .NET). Safe to delete.

## Version drift between gradle.properties and build.gradle.kts

**State**: `drift`
**Files**: `gradle.properties:26-27` ↔ `build.gradle.kts:9-10`

Two plugins are pinned in both places:

| Plugin | gradle.properties | build.gradle.kts | Status |
|---|---|---|---|
| `intellijPlatformGradlePluginVersion` | 2.14.0 | 2.14.0 | currently synced |
| `gradleJvmWrapperVersion` | 0.15.0 | 0.16.0 | **drifted** — `gradle.properties` value is dead |

The inline declaration in `build.gradle.kts` wins. The `gradle.properties` value gets read by `pluginManagement` in `settings.gradle.kts` for the *default* version — but the root build script then overrides with its own. Net effect: `gradle.properties` for these is misleading. §07's table flags this; §24 has the consolidation refactor.

## `.run/Build Plugin.run.xml` references stale JDK

**State**: `drift`
**File**: `.run/Build Plugin.run.xml:8`

References `corretto-17.0.7` while the project's toolchain is JDK 21. Importing this run config in IntelliJ either silently downgrades or fails. Should be updated to `corretto-21` or have the env var removed entirely (let the toolchain handle it).

## `.run/Build ReSharper Plugin.run.xml` invokes legacy PowerShell

**State**: `stale-delete`
**File**: `.run/Build ReSharper Plugin.run.xml`

Calls `buildPlugin.ps1`, which is a legacy script not used by CI. Should be replaced with a Gradle run config calling `:buildResharperPlugin`. Together with deleting the `.ps1` files, this is §24.3.

## `${buildDir}` is deprecated in Gradle 9, removed in 10

**State**: `workaround` (will become `breakage` on Gradle 10)
**File**: `build.gradle.kts:110`

Gradle deprecated `${buildDir}` (string-interpolated property) in favour of `layout.buildDirectory.dir(...)` (lazy `Provider<Directory>`). Used at `:110` inside `tasks.buildPlugin { doLast { copy { from("${buildDir}/distributions/...") ... } } }`. Will warn loudly with `--warning-mode all`; bumps to Gradle 10 will break it.

The fix is straightforward:

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

§24 has this captured.

## `tasks.patchPluginXml` reads CHANGELOG.md eagerly

**State**: `workaround`
**File**: `build.gradle.kts:239-247`

The `file("${rootDir}/CHANGELOG.md").readText()` call runs at *configuration time* — every Gradle invocation reads the file, even no-op ones. Wrap in `provider { }` for laziness:

```kotlin
changeNotes.set(provider {
    val changelogText = file("${rootDir}/CHANGELOG.md").readText()
        .lines()
        // ... existing parse ...
    "<ul>\r\n$changelogText\r\n</ul>"
})
```

Captured in §24.

## `untilBuild = null`

**State**: `intentional`, with risk
**File**: `build.gradle.kts:251`

`untilBuild.set(provider { null })` clears IPGP's auto-computed upper-bound on Rider compatibility. Default is to set `until-build` to the same major.minor as the SDK, blocking the plugin from loading on EAPs of the next version.

The maintainer's choice: opt for "always loadable" because (a) this plugin rarely hits binary breakage, (b) blocking users on legitimate upgrades is annoying, (c) when JetBrains does break something, the maintainer would rather get bug reports than have users blocked.

The trade-off: when JetBrains ships a Rider that breaks the plugin's API surface, *users hit it* instead of being told the plugin is incompatible. Risk-acceptance, not negligence.

## Empty Tests project + zero Kotlin tests

**State**: `gap`
**Files**: `src/dotnet/ReSharperPlugin.RimworldDev.Tests/ReSharperPlugin.RimworldDev.Tests.csproj` (no .cs files), no `src/rider/test/` directory

Test infrastructure exists in skeleton form but no tests are wired up:
- `Tests.csproj` references `Lib.Harmony` and the main plugin csproj but contains zero `.cs` files. Only `test/data/nuget.config` exists.
- No Kotlin frontend tests. No `BasePlatformTestCase` / `RiderTestBase` references in the codebase.
- The `testDotNet` Gradle task and `:publishPlugin` gate exist; on this stub they're effectively no-ops.

The `example-mod/` directory is a real fixture mod loaded by `runIde` as a manual fixture. Convertible to an integration test fixture with effort. The maintainer's stated goal — see §24.12.

## PowerShell scripts (mostly legacy)

**State**: mostly `stale-delete`
**Files**: `buildPlugin.ps1`, `publishPlugin.ps1`, `settings.ps1`, `tools/vswhere.exe`, `tools/nuget.exe`

Inherited from a JetBrains template. **Not used by CI.** They duplicate functionality already provided by Gradle (`:buildResharperPlugin`, the `dotnet nuget push` step in `Deploy.yml`). Deletion candidates.

The exception is `runVisualStudio.ps1` — it sets up an experimental ReSharper hive inside Visual Studio for local ReSharper-for-VS development. That's a real use case Gradle doesn't cover. Decision deferred (§24.3 leaves it to the maintainer).

## Three `intellijPlatform { }` scopes in one file

**State**: `intentional`
**Files**: `build.gradle.kts:47-50` (inside `repositories`), `:117-127` (inside `dependencies`), `:130-139` (top-level)

Each is the IPGP DSL scoped to a different container:
- Inside `repositories { }`: configure where to fetch the SDK from
- Inside `dependencies { }`: declare the SDK and bundled-plugin dependencies
- Top-level: configure plugin-wide settings (verifier, etc.)

They look like duplicates but configure different things. Not a smell.

## `useInstaller = false` and `useBinaryReleases = false`

**State**: `intentional`, tightly coupled
**Files**: `build.gradle.kts:120` and `gradle.properties:31`

Together they tell IPGP: download the Rider SDK as Maven artifacts (JARs in `intellij-repository/releases`), not as a binary installer/CDN tarball. The Maven path:
- Plays nicely with `actions/setup-java cache: gradle` in CI
- Works through JetBrains' cache-redirector mirrors
- Gives an extracted layout that this build's `riderModel` configuration can read from

Don't flip one without the other; they're coupled via the artifact path expectations downstream.

## `useBinaryReleases` lives in gradle.properties as a `buildFeature` flag

**State**: `intentional`, may rename
**File**: `gradle.properties:31`

`org.jetbrains.intellij.platform.buildFeature.useBinaryReleases=false` is an IPGP buildFeature flag. These flags can be renamed/removed across IPGP versions. If you upgrade IPGP and the build dies with "could not resolve com.jetbrains.intellij.rider:rider:2026.1", check the IPGP changelog for renames of this flag.

## `kotlin.stdlib.default.dependency=false`

**State**: `intentional`, do not change
**File**: `gradle.properties:21`

The IDE bundles a Kotlin stdlib at runtime. If Gradle pulls in another (because the Kotlin plugin auto-adds one by default), you get duplicate-class loader fights at plugin load time. This setting suppresses the auto-add. Documented in the file itself with a link to Kotlin 1.4 release notes.

## `org.gradle.jvmargs=-Xmx4g`

**State**: `intentional`, do not lower
**File**: `gradle.properties:22`

IPGP sandbox extraction is memory-hungry. Below 4 GB you'll OOM on the SDK extraction step. Don't lower without testing.

## `instrumentCode/instrumentTestCode disabled`

**State**: `intentional`
**File**: `build.gradle.kts:74-80`

Disables IPGP's NotNull annotation instrumentation. Historical: instrumentation used to choke on rdgen-generated bytecode and on plugins without `.form` files. This plugin has neither problem, but the disable is harmless and avoids a class of CI flakes.

## `riderModel` configuration arcanity

**State**: `intentional`, canonical JetBrains
**File**: `build.gradle.kts:254-269`, `protocol/build.gradle.kts:11-18`

The custom Configuration / artifacts / `builtBy(INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)` dance is canonical JetBrains plugin pattern. See §13. Not a smell, but obscure if you've never seen it.

→ End of Part 2. Next: [18 · Recipes](../part-3-operate/18-recipes.md)
