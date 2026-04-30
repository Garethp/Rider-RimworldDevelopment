# 04 · Gradle 201 — providers and config cache

**[Foundation]**

The half-step from "I can read tasks" to "I can edit them without breaking things." If you skip this, every `provider { }` and `Property<T>` and `argumentProviders` reference will feel like incantation.

## Why lazy values exist

Gradle has two kinds of value APIs: eager and lazy.

**Eager** (the old way):
```kotlin
val outputDir = file("build/output")    // resolved NOW, at configuration time
```

**Lazy** (the modern way):
```kotlin
val outputDir = layout.buildDirectory.dir("output")    // a Provider<Directory>, resolved later
```

Why bother? Three reasons:

1. **Configuration-time work shrinks** — eager file lookups, network calls, string interpolation all run on every build, even no-op ones. Lazy values defer that until the value is actually needed.
2. **The configuration cache** can serialize the task graph between runs. To do that, it has to know what's a "live" reference (forbidden in cached state) vs. a captured value (allowed). Providers are first-class to that machinery.
3. **Task-to-task wiring** — a downstream task can consume an upstream task's `Provider<File>` without knowing whether the upstream task has actually run yet. Gradle resolves the chain at execution time.

## `Property<T>` and `.set(...)`

The cliché Gradle DSL pattern:

```kotlin
tasks.publishPlugin {
    token.set(PublishToken)            // NOT  token = PublishToken
}

tasks.patchPluginXml {
    pluginVersion.set(PluginVersion)
    untilBuild.set(provider { null })
}
```

`token`, `pluginVersion`, `untilBuild` are all `Property<T>` (a `Provider<T>` that's also writable). You set them with `.set(...)`. The argument can be a literal value, another `Provider<T>`, or a `provider { ... }` lambda. Lambdas re-evaluate at use-time.

In this repo:
- `build.gradle.kts:235` — `token.set(PublishToken)` (`PublishToken` came from `by project`)
- `build.gradle.kts:249-251` — `pluginVersion.set(PluginVersion)`, `changeNotes.set(...)`, `untilBuild.set(provider { null })` (the `null` clears the auto-computed upper bound; see §17)

## `provider { }` — the escape hatch

When you have computation that should happen at use-time, not config-time:

```kotlin
artifacts {
    add(riderModel.name, provider {
        intellijPlatform.platformPath.resolve("lib/rd/rider-model.jar").also {
            check(it.isFile) { "..." }
        }
    }) {
        builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    }
}
```

`build.gradle.kts:259-268`. The `provider { ... }` is necessary because `intellijPlatform.platformPath` doesn't exist until IPGP's initialize task has extracted the SDK. Resolving the path at configuration time would crash; resolving it inside `provider { ... }` defers until it's available.

## `argumentProviders` — same idea, for command-line arguments

```kotlin
tasks.runIde {
    argumentProviders += CommandLineArgumentProvider {
        listOf("${rootDir}/example-mod/AshAndDust.sln")
    }
}
```

`build.gradle.kts:151-153`. `CommandLineArgumentProvider` is a SAM (single-method) interface. The lambda runs at execution time, not config time. Why use it for a static-looking string? Two reasons: idiomatic for IPGP, and config-cache-friendly.

## The configuration cache

Run `./gradlew :buildPlugin --configuration-cache` and Gradle will:

1. On first run, serialize the entire task graph (configurations, task wiring, captured values) to disk
2. On subsequent runs with the same inputs, **skip the configuration phase entirely** and just execute

This is huge for IDE plugin builds because configuration can take 5-15 seconds. The catch: certain things can't be cached.

**Cache-hostile patterns** (don't do these):
- Reading `Project` inside `doLast` (`project.someProperty` in execution code)
- Reading `gradle`, `subprojects`, `tasks` inside execution-time code
- Reading files at config time and storing the contents (better: capture the file path as a `Provider`, read inside `doLast`)

**Cache-friendly patterns**:
- Capturing `File` references in a `val` at config time, using them in `doLast`
- Wrapping reads in `provider { }`
- Using `Property<T>` for task wiring

In this repo, `build.gradle.kts:108-111` captures `pluginZip` and `outputDir` as `val`s at config time and uses `.get().asFile` inside `doLast`:

```kotlin
val pluginZip = layout.buildDirectory.file("distributions/${rootProject.name}-${version}.zip")
val outputDir = layout.projectDirectory.dir("output").asFile
doLast {
    val zipFile = pluginZip.get().asFile
    outputDir.mkdirs()
    // ...
}
```

This is the *correct* shape: file paths are determined at config time (fine, they're stable), file content / existence is checked at execution time (cache-safe).

## Up-to-date checks (incrementality)

Gradle decides whether to skip a task based on its declared inputs and outputs. A task with:

```kotlin
@InputDirectory val src = "src/dotnet"
@OutputDirectory val out = "src/dotnet/.../bin"
```

…will skip if no input file changed since the last run. A task with no declared inputs/outputs will run every time.

`compileDotNet`, `buildResharperPlugin`, and `testDotNet` in this repo are `Exec` tasks with no declared inputs/outputs (`build.gradle.kts:86-105`, `:226-230`). They always re-run. The `dependsOn(compileDotNet)` on `prepareSandbox` orders them, but doesn't make `compileDotNet` skippable. This is a known limitation, captured as a refactor opportunity in §24.

## Why this matters before reading the build file

When you encounter `something.set(provider { ... })`, `argumentProviders +=`, `val foo by tasks.registering(...)`, `extra["bar"] = ...`, or `pluginZip.get().asFile`, you'll know what shape of API you're looking at, why it's that shape, and what's safe to change.

→ Next: [05 · The cast of tools](05-the-cast-of-tools.md)
