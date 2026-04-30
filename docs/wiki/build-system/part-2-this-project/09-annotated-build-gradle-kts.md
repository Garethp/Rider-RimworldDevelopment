# 09 · Annotated build.gradle.kts

**[This Project]** — *The centerpiece. Every block of `build.gradle.kts` walked through with explanation.*

This page is meant to be read with `build.gradle.kts` open beside you. Each section quotes the relevant lines, explains what's happening, and flags any "watch out for" issues. Line numbers are correct as of the worktree this wiki was authored in.

---

## Imports (`:1-4`)

```kotlin
import com.jetbrains.plugin.structure.base.utils.isFile
import org.apache.tools.ant.taskdefs.condition.Os
import org.jetbrains.intellij.platform.gradle.Constants
import org.jetbrains.kotlin.gradle.dsl.JvmTarget
```

- `isFile` — a Kotlin extension on `java.nio.file.Path` from `com.jetbrains.plugin.structure`, transitively pulled in by IPGP. Used at `:262` to assert `rider-model.jar` exists.
- `Os` — Ant. Yes, Apache Ant. Gradle ships Ant under the hood for legacy reasons; this is the canonical way to detect the OS family in a Gradle script. Used at `:22` for `isWindows`.
- `Constants` — IPGP's task-name constants object. Used at `:267` (`Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN`).
- `JvmTarget` — used at `:83` to set Kotlin's bytecode target.

---

## `plugins { }` block (`:6-11`)

```kotlin
plugins {
    id("java")
    alias(libs.plugins.kotlinJvm)
    id("org.jetbrains.intellij.platform") version "2.14.0"
    id("me.filippov.gradle.jvm.wrapper") version "0.16.0"
}
```

Three plugin-declaration shapes in one block:
- `id("java")` — built-in Gradle plugin, no version needed
- `alias(libs.plugins.kotlinJvm)` — version-catalog reference (resolves through `gradle/libs.versions.toml`'s `[plugins]` table)
- `id("...") version "..."` — explicit version declaration

**Watch out:** the IPGP and jvm-wrapper plugin versions are *also* declared in `settings.gradle.kts` `pluginManagement.plugins` block, sourced from `gradle.properties`. Today the IPGP versions match (both 2.14.0); the jvm-wrapper ones don't (0.16.0 here vs 0.15.0 in `gradle.properties`). Real drift. The inline value (here) wins. See §07.

---

## `java { }` block (`:14-20`)

```kotlin
java {
    sourceCompatibility = JavaVersion.VERSION_21
    toolchain {
        languageVersion = JavaLanguageVersion.of(21)
    }
}
```

Two things:
- `sourceCompatibility` says "the .java sources in this project are written for Java 21"
- `toolchain` says "Gradle, find me a JDK 21 to use" (auto-provisions one if not present)

The toolchain is what actually picks the JDK. `sourceCompatibility` mostly affects javac — and there are no `.java` sources here. Effectively redundant but harmless.

`tasks.compileKotlin { compilerOptions { jvmTarget.set(JvmTarget.JVM_21) } }` at `:82-84` is a *third* JVM-target declaration, this time for Kotlin → bytecode. All three (toolchain, sourceCompatibility, JvmTarget) need to align. They do.

---

## `extra["isWindows"]` (`:22-23`)

```kotlin
val isWindows = Os.isFamily(Os.FAMILY_WINDOWS)
extra["isWindows"] = isWindows
```

`extra` is Gradle's loose property bag on the `Project` — a `MutableMap<String, Any?>`. Storing `isWindows` here lets later blocks read it without recomputing. Used at `:188` in the DotFiles patcher.

---

## `String by project` properties (`:25-31`)

```kotlin
val DotnetSolution: String by project
val BuildConfiguration: String by project
val ProductVersion: String by project
val DotnetPluginId: String by project
val RiderPluginId: String by project
val PublishToken: String by project
val PluginVersion: String by project
```

Property delegates that read from `gradle.properties` (or `-P` CLI overrides). Eager read at configuration time — if any property is missing, the build fails the moment that line executes.

`PublishToken` defaults to `"_PLACEHOLDER_"` in `gradle.properties:11`; CI overrides it via `-PPublishToken=$secret` (Deploy.yml).

`RiderPluginId` is declared but not actually read in this file — only used to inject as a property elsewhere. Harmless echo.

---

## Repositories (`:33-51`)

Two blocks:

**`allprojects.repositories`** (`:33-40`) — shared with `:protocol`:

```kotlin
allprojects {
    repositories {
        maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
        maven("https://cache-redirector.jetbrains.com/intellij-repository/releases")
        maven("https://cache-redirector.jetbrains.com/intellij-repository/snapshots")
        maven("https://cache-redirector.jetbrains.com/maven-central")
    }
}
```

**Top-level `repositories`** (`:42-51`) — for the root project, with IPGP-specific extras:

```kotlin
repositories {
    // (same four mavens)
    intellijPlatform {
        defaultRepositories()
        jetbrainsRuntime()
    }
}
```

`intellijPlatform { defaultRepositories() }` is IPGP's DSL adding the JetBrains-specific repos needed to resolve a Rider SDK artifact. `jetbrainsRuntime()` adds the JBR repo. Both are required for the `dependencies { intellijPlatform { rider(...); jetbrainsRuntime() } }` block at `:117-127` to resolve.

The duplication (mavens listed in both `allprojects` and root) is harmless. The version-catalog-style `dependencyResolutionManagement` in `settings.gradle.kts:47-57` is a more modern alternative; the current shape works.

---

## `apply { plugin("kotlin") }` (`:53-55`)

```kotlin
apply {
    plugin("kotlin")
}
```

**Redundant.** The Kotlin JVM plugin was already applied via `alias(libs.plugins.kotlinJvm)` at `:8`. This is dead cargo-cult code from a template ancestor. Flagged for cleanup in §17.

---

## `tasks.wrapper { }` (`:57-62`)

```kotlin
tasks.wrapper {
    gradleVersion = "9.4.1"
    distributionType = Wrapper.DistributionType.ALL
    distributionUrl =
        "https://cache-redirector.jetbrains.com/services.gradle.org/distributions/gradle-${gradleVersion}-all.zip"
}
```

Configures the built-in `wrapper` task. Running `./gradlew wrapper` regenerates `gradle/wrapper/gradle-wrapper.properties` to point at this version. The `cache-redirector` URL is JetBrains' mirror.

**Watch out:** the Gradle version is also pinned in `gradle/wrapper/gradle-wrapper.properties:3`. They must agree. After bumping, run `./gradlew wrapper` *twice* — that's the canonical Gradle dance to fully update the wrapper scripts.

---

## `version = ...` (`:64`)

```kotlin
version = extra["PluginVersion"] as String
```

Sets the project's version. Used by IPGP when constructing artifact filenames (e.g. `rimworlddev-2025.1.10.zip`).

---

## `sourceSets { }` (`:66-72`)

```kotlin
sourceSets {
    main {
        java.srcDir("src/rider/main/java")
        kotlin.srcDir("src/rider/main/kotlin")
        resources.srcDir("src/rider/main/resources")
    }
}
```

Overrides Gradle's default `src/main/{java,kotlin,resources}` paths to put everything under `src/rider/main/...`. Reason: the repo also holds `.NET` source under `src/dotnet/`, and segregating both languages by tier makes the layout legible.

`src/rider/main/java/` is empty in practice — there are no `.java` files. Could be deleted for tidiness; harmless if left.

---

## `instrumentCode/instrumentTestCode disabled` (`:74-80`)

```kotlin
tasks.instrumentCode { enabled = false }
tasks.instrumentTestCode { enabled = false }
```

IPGP runs IntelliJ's bytecode instrumentation on plugin classes (NotNull annotations, form `*.form` files). It's been a historical source of CI flakes for plugins that don't use `.form` files. Disabling is fine for this plugin.

---

## `compileKotlin compilerOptions` (`:82-84`)

```kotlin
tasks.compileKotlin {
    compilerOptions { jvmTarget.set(JvmTarget.JVM_21) }
}
```

Tells the Kotlin compiler to emit JVM 21 bytecode. Aligns with `java { toolchain { 21 } }` at `:14-20`.

---

## `compileDotNet` task (`:86-90`)

```kotlin
val compileDotNet by tasks.registering(Exec::class) {
    executable("dotnet")
    workingDir(rootDir)
    args("build", "--consoleLoggerParameters:ErrorsOnly", "--configuration", "Release")
}
```

The simplest of the .NET escape-hatch tasks. Just runs `dotnet build` from the repo root with errors-only logging.

**Watch out:** no `@InputDirectory` / `@OutputDirectory` declarations. Gradle has no idea what files go in or come out. Consequence: `compileDotNet` re-runs every build. `dependsOn(compileDotNet)` from other tasks orders correctly but doesn't make this skippable. Captured as refactor opportunity in §24.

---

## `buildResharperPlugin` task (`:92-105`)

```kotlin
val buildResharperPlugin by tasks.registering(Exec::class) {
    val arguments = mutableListOf<String>()
    arguments.add("msbuild")
    arguments.add(DotnetSolution)
    arguments.add("/t:Restore;Rebuild;Pack")
    arguments.add("/v:minimal")
    arguments.add("/p:PackageOutputPath=\"$rootDir/output\"")
    arguments.add("/p:PackageVersion=$PluginVersion")
    executable("dotnet")
    args(arguments)
    workingDir(rootDir)
}
```

Calls `dotnet msbuild` with three MSBuild targets (`Restore`, `Rebuild`, `Pack`) and two property overrides. The `Pack` target produces the `.nupkg` for the **Wave/ReSharper** flavour of the plugin (the .Rider csproj has `<IsPackable>false</IsPackable>` and won't pack). The output goes to `output/ReSharperPlugin.RimworldDev.<PluginVersion>.nupkg`.

This task is invoked by `Deploy.yml` to produce the ReSharper-marketplace artifact, but it's *not* part of `:buildPlugin`'s dependency chain — it's only run when explicitly requested (or by `Deploy.yml`).

Same incrementality caveat as `compileDotNet`.

---

## `tasks.buildPlugin { doLast { copy(...) } }` (`:107-114`)

```kotlin
tasks.buildPlugin {
    doLast {
        copy {
            from("${buildDir}/distributions/${rootProject.name}-${version}.zip")
            into("${rootDir}/output")
        }
    }
}
```

`buildPlugin` is contributed by IPGP — it produces the Rider plugin ZIP at `build/distributions/<name>-<version>.zip`. This `doLast` copies the ZIP into `output/` so `Deploy.yml`'s GitHub Release upload can find it.

**Watch out:**
- `${buildDir}` is **deprecated in Gradle 9** and will be **removed in Gradle 10**. Replace with `layout.buildDirectory.dir(...)` (a `Provider<Directory>`). Flagged in §17.
- The `copy { }` here is `Project.copy(Action)`, an eager Gradle API, not the `Copy` task type. Acceptable inside `doLast`.

---

## `dependencies { intellijPlatform { ... } }` (`:116-128`)

```kotlin
dependencies {
    intellijPlatform {
        rider(ProductVersion) {
            useInstaller = false
        }
        jetbrainsRuntime()
        bundledPlugin("com.intellij.resharper.unity")
        bundledModule("intellij.spellchecker")
    }
}
```

This is IPGP's DSL inside the `dependencies { }` block. What each line does:

- `rider(ProductVersion)` — declare a dependency on the Rider IDE artifact at version `2026.1` (from `gradle.properties:17`). IPGP downloads, extracts, and wires it onto the compile classpath.
- `useInstaller = false` — fetch from the JetBrains Maven repo (where artifact JARs live) rather than the binary CDN (where Rider installers live). The Maven path works with `actions/setup-java` Gradle caching in CI. Coupled with the `useBinaryReleases=false` flag in `gradle.properties:31`.
- `jetbrainsRuntime()` — pull in JBR (the matching JetBrains Runtime). Required because `runIde` launches a sandbox Rider, which needs JBR.
- `bundledPlugin("com.intellij.resharper.unity")` — make the Unity plugin's API surface available at compile time. The plugin descriptor (`plugin.xml:8`) declares a `<depends>` on this.
- `bundledModule("intellij.spellchecker")` — same idea, but the spellchecker has been moved from a "bundled plugin" to a "bundled module" in recent Platform versions. Module vs. plugin: bundled plugins have their own `plugin.xml` and an id; bundled modules are JAR-level units in `lib/modules/`.

**Watch out:** the bundled-plugin → bundled-module migration is the kind of thing that breaks silently on a Platform upgrade. If you upgrade to a Platform version where `intellij.spellchecker` becomes something else, this line fails. JetBrains documents these migrations in IPGP changelogs; consult before bumping.

---

## `intellijPlatform { pluginVerification { ... } }` (`:130-139`)

```kotlin
intellijPlatform {
    pluginVerification {
        freeArgs = listOf("-mute", "TemplateWordInPluginId")
        ides {
//            ide(IntelliJPlatformType.Rider, ProductVersion)
            recommended()
        }
    }
}
```

This is the **third** `intellijPlatform { }` scope you'll meet in this file (the others are inside `repositories { }` and `dependencies { }`). The bare top-level form configures plugin-wide settings.

`pluginVerification` controls IPGP's `verifyPlugin` task, which runs JetBrains' Plugin Verifier against your built artifact across multiple IDE versions to catch binary-incompatibility issues.

- `freeArgs = listOf("-mute", "TemplateWordInPluginId")` — silence a known false-positive rule that flags plugin IDs starting with "RimworldDev" because they happen to contain words from the JetBrains template
- `ides { recommended() }` — verify against JetBrains' "recommended" set of IDE versions. The commented-out alternative would pin a specific IDE+version

`verifyPlugin` is not run on every build. CI doesn't currently invoke it. You can run it locally with `./gradlew verifyPlugin`.

---

## `tasks.runIde { }` (`:141-154`)

```kotlin
tasks.runIde {
    dependsOn(compileDotNet)
    maxHeapSize = "1500m"
    autoReload = false
    argumentProviders += CommandLineArgumentProvider {
        listOf("${rootDir}/example-mod/AshAndDust.sln")
    }
}
```

Configures IPGP's `runIde` task — the one that boots a sandboxed Rider with this plugin loaded.

- `dependsOn(compileDotNet)` — ensure the .NET half is built first
- `maxHeapSize = "1500m"` — match Rider's default; IPGP's default of 512m is too small for Rider
- `autoReload = false` — Rider's backend doesn't support dynamic plugin reload (the .NET process can't safely swap code mid-flight). Disabling avoids a misleading auto-reload story
- `argumentProviders += CommandLineArgumentProvider { ... }` — pass `${rootDir}/example-mod/AshAndDust.sln` as an arg to the launched Rider, so it auto-opens the example mod

The `argumentProviders` shape is Gradle's lazy-args idiom (§04). The lambda runs at execution time, capturing `rootDir` from configuration time.

---

## `tasks.prepareSandbox { }` (`:156-224`)

The biggest, fragilest block. Has its own page: see §14.

In short: this task copies the .NET DLLs (and ProjectTemplates) into the sandboxed plugin folder, asserts the DLLs exist (failing the build if `compileDotNet` produced nothing), and on Mac/Linux patches missing Unity DotFiles DLLs from a local Rider install.

---

## `testDotNet` task (`:226-230`)

```kotlin
val testDotNet by tasks.registering(Exec::class) {
    executable("dotnet")
    args("test", DotnetSolution, "--logger", "GitHubActions")
    workingDir(rootDir)
}
```

`dotnet test` against the solution, with the GitHubActions logger format (which the CI workflow ingests for nice annotations).

Currently a near-no-op because `ReSharperPlugin.RimworldDev.Tests` is a stub (no `.cs` test files). The runner exits successfully because there's nothing to fail.

---

## `tasks.publishPlugin { }` (`:232-236`)

```kotlin
tasks.publishPlugin {
    dependsOn(testDotNet)
    dependsOn(tasks.buildPlugin)
    token.set(PublishToken)
}
```

Configures IPGP's `publishPlugin` task to upload the built ZIP to the JetBrains Marketplace.

- `dependsOn(testDotNet)` — gate publish on .NET tests (currently a no-op gate; will become real when tests exist)
- `dependsOn(tasks.buildPlugin)` — ensure the ZIP exists
- `token.set(PublishToken)` — Marketplace auth token. The default is `"_PLACEHOLDER_"`; CI overrides via `-PPublishToken=$secret`

`token.set(...)` is the `Property<T>` write idiom (§04). Don't try `token = "..."`.

---

## `tasks.patchPluginXml { }` (`:238-252`)

```kotlin
tasks.patchPluginXml {
    val changelogText = file("${rootDir}/CHANGELOG.md").readText()
        .lines()
        .dropWhile { !it.trim().startsWith("##") }
        .drop(1)
        .takeWhile { !it.trim().startsWith("##") }
        .filter { it.trim().isNotEmpty() }
        .joinToString("\r\n") {
            "<li>${it.trim().replace(Regex("^\\*\\s+?"), "")}</li>"
        }.trim()

    pluginVersion.set(PluginVersion)
    changeNotes.set("<ul>\r\n$changelogText\r\n</ul>")
    untilBuild.set(provider { null })
}
```

`patchPluginXml` is contributed by IPGP — it rewrites the plugin's `META-INF/plugin.xml` at build time with values you set here.

- The Kotlin chain parses `CHANGELOG.md`: skip everything before the first `##` heading, take the section between it and the next `##`, strip blank lines, convert each surviving line into an `<li>` HTML item. The result becomes the plugin's "what's new" block on the Marketplace.
- `pluginVersion.set(PluginVersion)` — overrides the `<version>0.0.0</version>` placeholder in `plugin.xml`
- `untilBuild.set(provider { null })` — clears the auto-computed upper-build-number bound. By default IPGP pins `until-build` to roughly the same major.minor as the SDK; setting it to null means the plugin claims to be compatible with all future Rider versions. **Risky in theory but pragmatic.** Trade-off is discussed in §17.

**Watch out:** the `file(...).readText()` runs at *configuration time*. That means every Gradle invocation reads `CHANGELOG.md`, even no-op ones. Ideally wrap in `provider { }` for laziness and config-cache friendliness. Flagged in §17.

---

## `riderModel` configuration + `artifacts { }` (`:254-269`)

The deepest Gradle rabbit hole in the project. Has its own page: see §13.

In short: this declares a custom Gradle `Configuration` named `riderModel` exposing `lib/rd/rider-model.jar` from the extracted Rider SDK to the `:protocol` subproject. The `builtBy(INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)` line ensures the SDK is downloaded before consumers try to read the JAR.

---

## End

That's every block. The hardest concepts — `prepareSandbox` (§14), `riderModel` (§13), the dual-csproj pattern referenced by `compileDotNet`/`prepareSandbox` (§12), and rdgen (§10) — get their own pages.

→ Next: [10 · The protocol subproject and rdgen](10-the-protocol-subproject-and-rdgen.md)
