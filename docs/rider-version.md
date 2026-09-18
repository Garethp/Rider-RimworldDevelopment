# Targeting a Rider version

**One line:** `<SdkVersion>` at the top of the root `Directory.Build.props`, exact and in NuGet form (`2026.1.5.2`,
`2026.2.0-rc01`, `2026.3.0-eap02`). Everything else follows from it:

| Derived | How |
|---|---|
| `JetBrains.Lifetimes` / `RdFramework` / `Annotations` (compile references) | The `ReferenceSdkCoreLibraries` target in `Directory.Build.props` references whatever the SDK's own dependency graph resolved. ~150 SDK packages pin one exact version of each, which changes with every SDK line; there are no direct `PackageReference`s for them, so nothing has to be matched by hand |
| `WaveVersion` (ReSharper `.nupkg` dependency) | Regex over `SdkVersion`: `2026.3.0-eap02` → `263.0.0` (the Wave package only publishes `<wave>.0.0`) |
| Gradle `ProductVersion` (the Rider the frontend compiles against and `runIde` launches) | `build.gradle.kts` reads `<SdkVersion>` through `providers.fileContents` (so the configuration cache notices edits) and maps it to the Maven form: `2026.3.0-eap02` → `2026.3-EAP2-SNAPSHOT`, `2026.2.0-rc01` → `2026.2-RC1-SNAPSHOT`, `2026.2.0` → `2026.2`, otherwise unchanged. `-PProductVersion=…` overrides it |

`./gradlew versions` (the `RiderVersionsTask` in `buildSrc/src/main/kotlin/rimworlddev/gradle/`, next to the
`RiderVersion` model that also holds the NuGet → Maven/Wave mappings the root build uses) lists every build of the current EAP cycle and the latest release of the last three stable lines,
marking what you're on; `./gradlew versions --to <target>` switches (it just rewrites `<SdkVersion>`), and
`./gradlew versions --usage` prints what `--to` accepts (Gradle intercepts `--help` itself; `./gradlew help --task
versions` shows the same text).

```
$ ./gradlew versions -q
Rider versions (NuGet JetBrains.Rider.SDK / IntelliJ Platform):
  EAP     2026.3.0-eap02    2026.3-EAP2-SNAPSHOT
  EAP     2026.3.0-eap01    2026.3-EAP1-SNAPSHOT
  Stable  2026.2.2          2026.2.2
  Stable  2026.1.5.2        2026.1.5.2             <- current
  Stable  2025.3.5.2        2025.3.5.2
```

| `--to` | Switches to |
|---|---|
| `eap` | newest build of the current EAP cycle (the newest line with no stable release yet); fails if there isn't one |
| `eap3`, `eap03`, `rc1` | that build of the current EAP cycle — step through them to find which EAP broke something |
| `2026.2-eap5` | that build of another line's EAP cycle |
| `latest` | the newest stable release |
| `2026.1` | that line's newest stable release, or its newest prerelease if it has none yet |
| `2026.2.0-rc01` | exactly that version (must be published) |

The list comes from NuGet's `JetBrains.Rider.SDK` index. Each row is also checked against the JetBrains Maven repository
the Gradle side downloads Rider from and against the `Wave` package; both can lag NuGet (and Maven drops old EAP snapshots), and the row
says so ("not on JetBrains Maven") rather than letting a later Gradle resolve fail. Network access happens when the task runs, so the
configuration cache is unaffected; after a switch Gradle reports that `Directory.Build.props` changed and re-derives
`ProductVersion`. Run `versions --to` on its own, not in the same invocation as a build. Rider re-evaluates the solution by itself when `Directory.Build.props`
changes; Gradle picks the change up on its next invocation (the IDE's Gradle model needs a sync, as for any platform
change).

A one-off without editing, MSBuild side only: `dotnet build -p:SdkVersion=2026.3.0-eap02`. Trying a version is best done
in a separate worktree, so your main checkout's `obj/` isn't flipped between SDKs; each SDK version adds a few GB to the
NuGet cache.

## Still manual

Only when a new platform demands it:

- `gradle/libs.versions.toml` — Kotlin, the IntelliJ Platform Gradle Plugin, jvm-wrapper, and `rdGen`. `rdGen` shares a
  release train with JetBrains.Lifetimes and should equal the version the SDK resolves (see
  `obj/<project>/project.assets.json`), but only matters when regenerating the protocol (`./gradlew :protocol:rdgen`).
- The Gradle wrapper, JVM 21 (toolchain, `jvmTarget`, CI `java-version`).
- The .NET target frameworks and CI's `dotnet-version`, when Rider moves runtime.
- `plugin.xml`'s `since-build`, only when dropping support for older Rider (`untilBuild` is unset).

## Verified (2026-09-18), editing only `<SdkVersion>`

| SdkVersion | Resolved Lifetimes / Annotations | Gradle ProductVersion | Result |
|---|---|---|---|
| 2026.1.5.2 (current) | 2026.1.2 / 2025.2.0 | 2026.1.5.2 | builds; `buildPlugin` OK against RD-261.27258.82 |
| 2026.2.2 | 2026.2.5 / 2026.2.0 | 2026.2.2 | builds (and the `automated-tests` branch's suite passed unchanged) |
| 2026.3.0-eap02 | 2026.3.0 / 2026.2.0 | 2026.3-EAP2-SNAPSHOT | restores; the plugin hits real 2026.3 API breaks (`…AspectLookupItems.Matchers.DeclaredElementMatcher` removed, `ReparsedCodeCompletionContext.Range` inaccessible) |

Side effect of dropping the direct references: the ReSharper `.nupkg` no longer declares (leaked) dependencies on
JetBrains.Annotations/Lifetimes/RdFramework; its files and the `Wave` dependency are unchanged.
