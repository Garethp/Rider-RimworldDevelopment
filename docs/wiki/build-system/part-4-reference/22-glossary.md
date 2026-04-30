# 22 · Glossary

**[Reference]** — *One-line definitions for the obtuse vocabulary used throughout. Skim once; come back when you hit something unfamiliar.*

## Gradle terms

**Configuration**
A typed bucket of dependencies and/or artifacts on a project. Used for compile classpaths, runtime classpaths, and custom artifact channels (this repo's `riderModel` is a custom one).

**Configuration cache**
A Gradle 7.4+ feature that serializes the entire task graph between runs, skipping the configuration phase on subsequent builds. Requires careful coding (no live `Project` access at execution time). Enabled with `--configuration-cache`.

**Configuration phase**
The middle of Gradle's three-phase lifecycle: where `build.gradle.kts` runs top-to-bottom and tasks get registered/configured. Distinct from execution phase, which runs the task actions themselves.

**`dependsOn` / `mustRunAfter`**
Task ordering primitives. `dependsOn(t)` says "if I run, t must run first." Does not declare data flow — for that, see "incremental tasks."

**`Exec` task**
A built-in Gradle task type that runs a command-line process. Used in this repo for `compileDotNet`, `buildResharperPlugin`, `testDotNet`.

**`extra` / extension extra properties**
A loose `MutableMap<String, Any?>` attached to `Project` for ad-hoc properties. Used at `build.gradle.kts:23` for `extra["isWindows"]`.

**Incremental task**
A task with declared `@InputFiles`/`@OutputFiles` so Gradle can skip it when inputs haven't changed. Tasks without these declarations always re-run.

**Initialization phase**
The first of Gradle's three lifecycle phases — where `settings.gradle.kts` runs and subprojects are discovered.

**`pluginManagement`**
A block in `settings.gradle.kts` that controls plugin resolution: where plugins come from, what default versions, custom mappings.

**Project**
A Gradle entity per buildable directory. The root project + subprojects. Each has its own `build.gradle.kts`.

**`Property<T>`**
A writable `Provider<T>`. The `something.set(value)` idiom. Used for lazy task wiring — the value can be set or read at any time.

**`Provider<T>`**
A lazy value. Resolved on first read, often at execution time. The escape hatch for "compute this later" in modern Gradle.

**`registering` / `register`**
Lazy task registration. `val foo by tasks.registering(Exec::class) { ... }` registers a new task without configuring it eagerly. Modern preference over `tasks.create("foo", Exec::class) { ... }`.

**Repository**
A source of artifacts (Maven, Ivy, custom). Configured in `repositories { }` blocks.

**Settings script**
`settings.gradle.kts`. Runs at initialization, configures `pluginManagement`, declares subprojects.

**Source set**
A logical grouping of sources (e.g. `main`, `test`). This repo's `main` source set is rebound to `src/rider/main/` instead of `src/main/`.

**Toolchain**
A JVM auto-provisioning mechanism. `java { toolchain { languageVersion = JavaLanguageVersion.of(21) } }` tells Gradle "find or download a JDK 21."

**Up-to-date check**
Gradle's per-task incrementality decision: skip if all `@InputFiles` are unchanged since the last run.

**Version catalog**
The `gradle/libs.versions.toml` file. A typed centralization of version strings. Referenced from build scripts via the `libs` accessor (e.g. `alias(libs.plugins.kotlinJvm)`).

**`withType<T> { }`**
Lazy configuration of all current and future tasks of a given type. Used in `protocol/build.gradle.kts:48` to configure `RdGenTask`s.

## IntelliJ / Rider plugin terms

**Bundled module**
A JAR-level unit shipped inside the IDE under `lib/modules/`. Newer than bundled plugins. Some subsystems (like spellchecker in 2024.2+) have migrated from bundled plugin to bundled module status.

**Bundled plugin**
A plugin that ships pre-installed with the IDE, with its own `plugin.xml` and id. Declared as `bundledPlugin("...")` in IPGP.

**IPGP / IntelliJ Platform Gradle Plugin v2**
`org.jetbrains.intellij.platform`. The Gradle plugin that does ~70% of this build. Adds tasks, downloads SDKs, manages repos. Documented at <https://plugins.jetbrains.com/docs/intellij/tools-intellij-platform-gradle-plugin.html>.

**JBR (JetBrains Runtime)**
JetBrains' fork of OpenJDK shipped with IntelliJ-family IDEs. Required for `runIde` because Rider's bundled debugger and Mono integration rely on JBR-specific instrumentation.

**`patchPluginXml`**
IPGP task that rewrites `plugin.xml` with build-time values (version, change notes, since-build, until-build).

**`platformPath`**
IPGP API: filesystem path to where the Rider SDK was extracted. Used at `build.gradle.kts:208, :261`. Only valid after IPGP's initialize task has run.

**Plugin verifier**
JetBrains' compatibility checker. Run via `./gradlew verifyPlugin`. Catches binary-incompat issues against multiple IDE versions.

**Plugin XML / `plugin.xml`**
The plugin's manifest. Lives at `src/rider/main/resources/META-INF/plugin.xml`. Declares plugin id, dependencies, extension points.

**Sandbox**
A temporary IDE plugins directory (`build/idea-sandbox/plugins/<name>/`) prepared by `prepareSandbox` and loaded by `runIde`.

**`since-build` / `until-build`**
Plugin compatibility range, declared in `plugin.xml`. Build numbers like `261` = Rider 2026.1. This repo sets `until-build = null` (no upper bound) — see §17.

## Rider/.NET-specific terms

**`Directory.Build.props`**
An MSBuild file auto-imported into every `.csproj` in the directory tree. Centralizes .NET version pins, package references, output paths.

**RD (Reactive Distributed) framework**
JetBrains' typed RPC system used to bridge Rider's JVM frontend and .NET backend. Two libraries: `JetBrains.RdFramework` (.NET runtime), `JetBrains.Rd` (JVM runtime). Source: <https://github.com/JetBrains/rd>.

**rdgen**
The RD code generator. Reads a Kotlin DSL declaring protocol calls/signals/properties and emits matched Kotlin and C# bindings.

**ReSharper SDK / Rider SDK**
Two NuGet package families. `JetBrains.ReSharper.SDK` is the Wave/ReSharper-for-VS extension API. `JetBrains.Rider.SDK` is its Rider-specific superset.

**Wave**
ReSharper's internal version stream. Wave 261 = ReSharper 2026.1. Used by the Wave NuGet package as a compatibility constraint for ReSharper-for-VS plugins.

## Build / .NET terms

**`dotnet` CLI**
Microsoft's command-line tool for .NET. Wraps MSBuild, NuGet, the test runner.

**MSBuild**
The .NET build engine. Reads `.csproj`/`.sln` files, runs targets like `Restore`, `Build`, `Pack`. Invoked here through `dotnet msbuild`.

**NuGet**
.NET's package manager. PackageReferences in `.csproj` files declare dependencies; `dotnet restore` resolves them.

**`PackageReference`**
A `.csproj` element declaring a NuGet dependency. Modern alternative to `packages.config`.

**TFM (Target Framework Moniker)**
A short string like `net6.0`, `net8.0` declaring which .NET runtime a project targets. Set via `<TargetFramework>` in `.csproj`.

## Project-specific terms

**`AshAndDust.sln`**
The example mod's solution file. Passed to `runIde` so Rider auto-opens it.

**Dotfiles / DotFiles plugin**
JetBrains' Unity-debugger helper DLLs. Live under Rider's `plugins/rider-unity/DotFiles/`. Stripped from the cross-platform Maven artifact on Mac/Linux; copied in by the workaround in `build.gradle.kts:188-222`.

**Frontend / Backend**
This plugin's two halves. Frontend = JVM/Kotlin in Rider's UI process. Backend = .NET/C# in Rider's ReSharper-host process.

**Remodder**
A plugin feature: decompiles Rimworld's compiled DLLs and shows transpilation results in a tool window. The reason for AsmResolver, ICSharpCode.Decompiler, Lib.Harmony NuGet refs.

**`riderModel` configuration**
A custom Gradle Configuration in this repo (`build.gradle.kts:254-269`) that exposes `lib/rd/rider-model.jar` from the extracted Rider SDK to the `:protocol` subproject. See §13.

→ Next: [23 · Where to ask JetBrains](23-where-to-ask-jetbrains.md)
