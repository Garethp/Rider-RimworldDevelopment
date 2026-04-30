# 05 · The cast of tools

**[Foundation]**

This build invokes about a dozen distinct tools. Here's a one-paragraph role for each. When the rest of the wiki name-drops one, come back here for the elevator pitch.

## Build orchestration

**Gradle** — the umbrella. Reads `build.gradle.kts` and `settings.gradle.kts`, builds the task graph, runs tasks in order. The wrapper script (`gradlew` / `gradlew.bat`) downloads the right Gradle version automatically; this repo pins Gradle 9.4.1 (`gradle/wrapper/gradle-wrapper.properties:3` and `build.gradle.kts:58`).

**`me.filippov.gradle.jvm.wrapper`** — a Gradle plugin that bundles a JDK with the `gradlew` scripts so a fresh clone bootstraps even on a machine without JDK 21 installed. Probably removable now that Gradle has its own toolchain auto-provisioning (foojay-resolver-convention) — flagged in §24.

## JVM / Kotlin side

**Kotlin Gradle Plugin** — compiles Kotlin sources to JVM bytecode. Pulled in via `alias(libs.plugins.kotlinJvm)` (`build.gradle.kts:8`). Version pinned in `gradle/libs.versions.toml:2`.

**IntelliJ Platform Gradle Plugin (IPGP) v2** — the giant. Knows how to download Rider from the JetBrains Maven repo, extract the SDK, register tasks like `prepareSandbox` / `runIde` / `buildPlugin` / `patchPluginXml` / `publishPlugin` / `verifyPlugin`, package the final ZIP in the right shape, and upload to the Marketplace. Roughly 70% of this build's "it just works" comes from IPGP. Plugin id: `org.jetbrains.intellij.platform`. Documented at <https://plugins.jetbrains.com/docs/intellij/tools-intellij-platform-gradle-plugin.html>.

**JBR (JetBrains Runtime)** — JetBrains' fork of OpenJDK that ships with IntelliJ-family IDEs. The sandboxed Rider that `runIde` launches needs JBR specifically (not Corretto), because the bundled debugger and Mono integration assume JBR's instrumentation. Wired by `jetbrainsRuntime()` in `build.gradle.kts:49, :123`.

## Cross-tier protocol

**rdgen / RD framework** — JetBrains' "Reactive Distributed" RPC system. You write a single Kotlin DSL file declaring calls/signals/properties; rdgen produces matched Kotlin and C# bindings; the RdFramework runtime libraries (in both languages) marshal calls between the JVM and .NET halves. The `:protocol` subproject in this repo is dedicated to running rdgen. Lives at <https://github.com/JetBrains/rd>.

## .NET / C# side

**.NET SDK** — Microsoft's `dotnet` CLI. Builds the C# half. This repo pins .NET SDK 7.0.202 with `rollForward: latestMajor` in `global.json`. Gradle's `compileDotNet` task is just `dotnet build`.

**MSBuild** — the build engine .NET uses, invoked through `dotnet msbuild` in the `buildResharperPlugin` task (`build.gradle.kts:92-105`). Reads `.csproj` files, applies `Directory.Build.props` automatically, runs targets like `Restore;Rebuild;Pack`.

**`Directory.Build.props`** — an MSBuild file at the repo root that's auto-imported into every `.csproj`. Centralizes the .NET SDK version, NuGet package versions, output paths. Equivalent role to `gradle.properties` for the .NET side.

**JetBrains.Rider.SDK / JetBrains.ReSharper.SDK** — NuGet packages providing the C# APIs to extend Rider's backend / ReSharper. Pinned to `$(SdkVersion)` in `Directory.Build.props:43, :40-41`.

**Wave** — ReSharper's internal version stream. Wave 261 = ReSharper 2026.1, Wave 252 = 2025.2. The Wave NuGet package is referenced by the ReSharper-flavor csproj for ReSharper-for-VS compatibility; the version is computed from `SdkVersion` in `Directory.Build.props:33-35`.

**AsmResolver, ICSharpCode.Decompiler, Lib.Harmony, Krafs.Publicizer** — third-party .NET libraries used by the "Remodder" feature (decompiling Rimworld's compiled code). Listed in `ReSharperPlugin.RimworldDev.Rider.csproj:27-35`. `prepareSandbox` manually copies the resulting DLLs into the plugin (`build.gradle.kts:160-171`).

## Tools that are *legacy* in this repo

**vswhere / nuget.exe / `tools/` directory** — bundled in the repo (`tools/vswhere.exe`, `tools/nuget.exe`) and called by `settings.ps1`. Used to find a Visual Studio install for the legacy ReSharper-for-VS local-dev flow via `runVisualStudio.ps1`. Not used by CI, not used by the Gradle build. The maintainer's stripping of vswhere from `compileDotNet` was a previous cleanup pass; the remnants survive in PowerShell scripts.

**`buildPlugin.ps1`, `publishPlugin.ps1`, `settings.ps1`** — PowerShell scripts at repo root. Inherited from a JetBrains template. **Not invoked by CI.** They duplicate functionality already provided by Gradle (`buildResharperPlugin`, `publishPlugin`). Candidates for deletion (§24). Only `runVisualStudio.ps1` has a unique role (set up an experimental ReSharper hive in Visual Studio).

## Glue you'll meet

**`example-mod/`** — a real Rimworld mod checked into the repo. `runIde` opens it as a manual fixture so you have something to poke at when developing. Future home of integration tests (§17).

**`.run/*.run.xml`** — IntelliJ run configurations checked into the repo. `Build Plugin.run.xml` runs `:buildPlugin -x compileDotNet`; `Build ReSharper Plugin.run.xml` invokes the legacy PowerShell. Both have stale references (`corretto-17.0.7`, the .ps1 path); §17 flags them.

→ End of Part 1. Next: [06 · Repo tour](../part-2-this-project/06-repo-tour.md)
