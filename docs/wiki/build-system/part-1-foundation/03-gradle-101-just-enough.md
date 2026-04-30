# 03 · Gradle 101 — just enough

**[Foundation]**

Just enough Gradle to read `build.gradle.kts` without panic. If you've used Maven but not Gradle, this page is for you. Skip if you've already built non-trivial Gradle projects.

## The unit of work: a *task*

Gradle is a graph of *tasks*. A task does one thing (compile some code, copy some files, zip a directory, run a test). Tasks have:

- A **name** (e.g. `compileKotlin`)
- A **type** (e.g. `Exec`, `Copy`, `Jar`, or a custom one)
- **Inputs** and **outputs** (files Gradle tracks for incrementality)
- **`dependsOn`** edges to other tasks

When you run `./gradlew runIde`, Gradle computes the closure of all tasks that need to run, orders them, and executes.

## The three lifecycle phases (this is the secret one)

This concept matters more than any other Gradle concept. Internalize it.

1. **Initialization** — Gradle reads `settings.gradle.kts`, figures out what subprojects exist
2. **Configuration** — Gradle runs `build.gradle.kts` top to bottom, **including bodies of `tasks.foo { ... }` blocks**, just to register and configure tasks
3. **Execution** — Gradle runs the actions inside the chosen tasks (the `doLast { }` and `doFirst { }` blocks, or the built-in actions of typed tasks like `Exec`)

```kotlin
val foo by tasks.registering(Exec::class) {
    println("A")              // runs at CONFIGURATION (every build, even if foo doesn't run)
    executable("dotnet")
    doLast {
        println("B")          // runs at EXECUTION (only when foo actually runs)
    }
}
```

Things you'll do that depend on knowing this:
- Reading a file with `file(...).readText()` at the top of `build.gradle.kts` happens **at configuration time** — every build pays for it (e.g. `tasks.patchPluginXml { ... }` in this repo reads `CHANGELOG.md` eagerly; flagged for a later refactor)
- Wrapping work in `doLast { }` defers it to execution time

## Tasks: register, named, configure

Three syntaxes, all common:

```kotlin
// Register a NEW task
val compileDotNet by tasks.registering(Exec::class) {
    executable("dotnet")
    args("build")
}

// Configure an EXISTING task (added by a plugin) — same as `tasks.named<T>("name") { ... }`
tasks.runIde {
    dependsOn(compileDotNet)
}

// Configure ALL tasks of a given type (now and in the future)
tasks.withType<RdGenTask> {
    classpath(sourceSets["main"].runtimeClasspath)
}
```

When you see `tasks.runIde { ... }` in this codebase, you are *not* registering `runIde` — you're configuring one that the IntelliJ Platform Gradle Plugin already added. The block is a configuration action.

## `dependsOn` is ordering, not data

```kotlin
tasks.runIde {
    dependsOn(compileDotNet)   // "run compileDotNet before runIde"
}
```

`dependsOn` says "if you're going to run me, run that first." It does **not** declare any data flow. To get incremental builds, the producer task must declare `@OutputFiles` and the consumer must consume those as inputs. `dependsOn` alone doesn't mean "Gradle knows compileDotNet's output is fresh."

This matters here: `compileDotNet` in this repo doesn't declare inputs/outputs (`build.gradle.kts:86-90`), so it always re-runs. Documented in §17 as a known issue, fixable later.

## The `plugins { }` block

Three shapes you'll see, all in `build.gradle.kts:6-11`:

```kotlin
plugins {
    id("java")                                                    // built-in
    alias(libs.plugins.kotlinJvm)                                 // version catalog reference
    id("org.jetbrains.intellij.platform") version "2.14.0"        // explicit version
    id("me.filippov.gradle.jvm.wrapper") version "0.16.0"
}
```

`alias(libs.plugins.kotlinJvm)` resolves through `gradle/libs.versions.toml`'s `[plugins]` table — Gradle's "version catalog" feature. It's just a typed pointer to the same `id(...) version "..."` declaration; the value is centralized.

## `by project` — the property delegate

Throughout `build.gradle.kts:25-31` you'll see:

```kotlin
val DotnetSolution: String by project
val PluginVersion: String by project
```

This is Kotlin property delegation. `by project` reads the property out of `gradle.properties` (or a CLI override like `-PPluginVersion=1.2.3`). The read is **eager** at configuration time — if the property is missing, the build fails the moment that line is evaluated.

(Also possible: `by settings`, used in `settings.gradle.kts:4-14` because the settings script doesn't have a Project, only a Settings.)

## `apply { plugin("...") }` — the older style

```kotlin
apply { plugin("kotlin") }
```

Equivalent to declaring `id("kotlin")` in the `plugins { }` block. The newer form is preferred. This repo's `build.gradle.kts:53-55` does both, redundantly — flagged for cleanup in §17.

## `extra` — Gradle's loose property bag

```kotlin
val isWindows = Os.isFamily(Os.FAMILY_WINDOWS)
extra["isWindows"] = isWindows
```

A `Project`-attached map for ad-hoc properties readable across blocks. Used in `build.gradle.kts:22-23` to share OS detection so other blocks don't redo it.

## What plugins contribute

A Gradle plugin can:
- Register tasks (e.g. IPGP adds `prepareSandbox`, `runIde`, `buildPlugin`, `patchPluginXml`, `publishPlugin`, `verifyPlugin`)
- Add DSL extensions (e.g. `intellijPlatform { ... }`)
- Add repositories
- Add dependencies / configurations
- Wire `dependsOn` chains

When you see `tasks.somethingYouNeverDefined { ... }` in `build.gradle.kts`, it's almost certainly contributed by a plugin. §21 has a full list of who contributes what.

## A `Provider<T>` is a lazy value

You'll see `something.set(provider { ... })` and `argumentProviders += CommandLineArgumentProvider { ... }`. Gradle has been moving toward lazy/deferred values everywhere. The "set this value" idiom isn't `something = value` but `something.set(value)`. Covered in detail in §04.

→ Next: [04 · Gradle 201 — providers and config cache](04-gradle-201-providers-and-config-cache.md)
