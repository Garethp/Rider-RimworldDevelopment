# 08 · settings.gradle.kts and pluginManagement

**[This Project]** — *Mostly standard Gradle, with one specific JetBrains-flavoured workaround.*

`settings.gradle.kts` is the script Gradle runs at *initialization* — before any `build.gradle.kts` files. It does three things in this repo:

1. Names the root project (`rootProject.name = "rimworlddev"`)
2. Configures `pluginManagement` (where Gradle finds plugins, and what versions)
3. Declares subprojects (`include(":protocol")`)

## The whole file at a glance

`settings.gradle.kts:1-59`. Reading it in five chunks:

### Chunk 1: project name (`:1`)

```kotlin
rootProject.name = "rimworlddev"
```

This is the name that ends up in `build/distributions/rimworlddev-<version>.zip` and the sandbox path `<rootProject.name>/dotnet/...` (used at `build.gradle.kts:175, 178`). Don't rename casually.

### Chunk 2: pluginManagement properties (`:3-14`)

```kotlin
pluginManagement {
    val rdVersion: String by settings
    val rdKotlinVersion: String by settings
    val intellijPlatformGradlePluginVersion: String by settings
    val gradleJvmWrapperVersion: String by settings
    // ... and several "echo" properties (DotnetPluginId, etc.)
```

`String by settings` is the same property delegate as `by project`, but reads from `gradle.properties` *during initialization*. The settings script doesn't have a `Project` object yet — only a `Settings` — so the delegate target is different.

The "echo" properties (`DotnetPluginId`, `DotnetSolution`, etc.) appear to be defensive: declared here but not actually read inside `pluginManagement`. Harmless but noise.

### Chunk 3: pluginManagement repositories (`:16-27`)

```kotlin
repositories {
    maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
    maven("https://cache-redirector.jetbrains.com/plugins.gradle.org")
    maven("https://cache-redirector.jetbrains.com/maven-central")
    maven("https://cache-redirector.jetbrains.com/dl.bintray.com/kotlin/kotlin-eap")
    maven("https://cache-redirector.jetbrains.com/myget.org.rd-snapshots.maven")
    maven("https://cache-redirector.jetbrains.com/intellij-dependencies")     // duplicate

    if (rdVersion == "SNAPSHOT") {
        mavenLocal()
    }
}
```

These tell Gradle *where to download Gradle plugins from*. All routed through JetBrains' cache redirector — a transparent CDN cache for upstream Maven repos. Works around upstream availability blips and gives JetBrains telemetry on what's being fetched.

The `if (rdVersion == "SNAPSHOT") mavenLocal()` line lets a JetBrains employee build against a locally-published rd snapshot. You will essentially never need this.

(There's a duplicate `intellij-dependencies` line — harmless but cleanup-able.)

### Chunk 4: pluginManagement plugin versions (`:29-34`)

```kotlin
plugins {
    id("com.jetbrains.rdgen") version rdVersion
    id("org.jetbrains.kotlin.jvm") version rdKotlinVersion
    id("org.jetbrains.intellij.platform") version intellijPlatformGradlePluginVersion
    id("me.filippov.gradle.jvm.wrapper") version gradleJvmWrapperVersion
}
```

This block declares **default versions** for plugins that subprojects might apply *without specifying a version themselves*. Notice `:protocol/build.gradle.kts:4` does:

```kotlin
plugins {
    id("org.jetbrains.kotlin.jvm")     // no version — picks up default from here
}
```

The root `build.gradle.kts:9-10` overrides with explicit `version "..."` for IPGP and jvm-wrapper. That's the duplication / drift surface (§07).

### Chunk 5: the rdgen coordinate hack (`:36-45`)

```kotlin
resolutionStrategy {
    eachPlugin {
        // Gradle has to map a plugin dependency to Maven coordinates - '{groupId}:{artifactId}:{version}'.
        // It tries to do '{plugin.id}:{plugin.id}.gradle.plugin:version'.
        // This doesn't work for rdgen, so we provide some help
        if (requested.id.id == "com.jetbrains.rdgen") {
            useModule("com.jetbrains.rd:rd-gen:${requested.version}")
        }
    }
}
```

Gradle's plugin marketplace convention says plugin id `foo` lives at Maven coordinates `foo:foo.gradle.plugin`. The rdgen plugin doesn't follow that convention — it's published at `com.jetbrains.rd:rd-gen`. This block tells Gradle: *"when somebody asks for plugin id `com.jetbrains.rdgen`, fetch from `com.jetbrains.rd:rd-gen` instead"*.

This is **canonical and required**, not a smell. You'll see the same pattern in every JetBrains-template repo using rdgen.

### Chunk 6: dependencyResolutionManagement (`:47-57`)

```kotlin
dependencyResolutionManagement {
    repositories {
        // ... same JetBrains cache-redirector mirrors
    }
}
```

Where Gradle finds **library dependencies** (as opposed to Gradle plugins). Centralizing it here keeps `build.gradle.kts` tidy.

### Chunk 7: subprojects (`:59`)

```kotlin
include(":protocol")
```

The single subproject. The root project is itself the JVM frontend (its sources are in `src/rider/main/`); `:protocol` is the rdgen runner. There is no `:rider-frontend` subproject — the root *is* it.

## Mental model

| pluginManagement block | dependencyResolutionManagement block |
|---|---|
| Where Gradle finds **plugins** | Where Gradle finds **libraries** |
| Plugin **default versions** | n/a |
| Plugin id → Maven coords mapping | n/a |
| Runs at **initialization** | Used during **configuration** |
| Reads `String by settings` from `gradle.properties` | n/a |

## Key takeaway

You'd think `pluginManagement` reads from the version catalog (`libs.versions.toml`). It can — Gradle 7.4+ permits some access — but historically didn't, which is why this build's IPGP and rdgen plugin versions live in `gradle.properties` and are read via `String by settings`. The constraint has loosened, but the migration to put them all in the version catalog is one of §24's refactor opportunities, not done yet.

→ Next: [09 · Annotated build.gradle.kts](09-annotated-build-gradle-kts.md)
