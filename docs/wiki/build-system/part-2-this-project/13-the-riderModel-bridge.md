# 13 · The `riderModel` bridge

**[This Project]** — *Idiomatic JetBrains pattern, but obscure if you've never seen it. Visible in `gradle-template`, `resharper-unity`, `azure-toolkit`. Worth understanding once.*

The deepest Gradle rabbit hole in the project. Fifteen lines of DSL implementing what's conceptually a one-line idea: *"the `:protocol` subproject needs `lib/rd/rider-model.jar` from inside the Rider SDK on its compile classpath."*

## The problem

`protocol/src/main/kotlin/model/rider/Model.kt:8` declares:

```kotlin
import com.jetbrains.rider.model.nova.ide.SolutionModel

object RemodderProtocolModel : Ext(SolutionModel.Solution) { ... }
```

`SolutionModel.Solution` is defined inside a JAR called `rider-model.jar`, which lives inside the Rider SDK that IPGP downloads. To compile `Model.kt`, that JAR must be on the `:protocol` compile classpath.

There's no published Maven artifact for `rider-model.jar` you could just `implementation(...)`. It only exists *inside* the extracted Rider SDK directory. So the build has to do something custom.

## The solution

The root project publishes a custom Gradle `Configuration` named `riderModel` that exposes the JAR. The `:protocol` module consumes from that configuration.

### Producer side: `build.gradle.kts:254-269`

```kotlin
val riderModel: Configuration by configurations.creating {
    isCanBeConsumed = true
    isCanBeResolved = false
}

artifacts {
    add(riderModel.name, provider {
        intellijPlatform.platformPath.resolve("lib/rd/rider-model.jar").also {
            check(it.isFile) {
                "rider-model.jar is not found at $riderModel"
            }
        }
    }) {
        builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)
    }
}
```

What each line does:

- `val riderModel: Configuration by configurations.creating { ... }` — creates a new Configuration on the root project named `riderModel`. A *Configuration* in Gradle is a typed bucket of dependencies and artifacts — used for compile classpaths, runtime classpaths, custom artifact channels.
- `isCanBeConsumed = true` — other projects can depend on this configuration and pull artifacts from it
- `isCanBeResolved = false` — this project itself can NOT use this configuration to resolve dependencies into a classpath. We're publishing a one-way channel.
- `artifacts { add(riderModel.name, provider { ... }) { ... } }` — register an artifact in the configuration. The artifact is a single file: `intellijPlatform.platformPath.resolve("lib/rd/rider-model.jar")`.
- The `provider { }` wrapper makes the file lookup lazy. Necessary because `intellijPlatform.platformPath` doesn't exist until IPGP's initialize task has extracted the SDK.
- `check(it.isFile) { ... }` is a fail-fast assertion. If the JAR isn't where we expect, you get a clear error instead of a baffling NoClassDefFoundError later.
- `builtBy(Constants.Tasks.INITIALIZE_INTELLIJ_PLATFORM_PLUGIN)` — tells Gradle this artifact is "produced by" the IPGP initialize task. Without this, Gradle would try to resolve the artifact before the SDK is downloaded — `rider-model.jar` doesn't exist yet, the assertion fires, build crashes.

### Consumer side: `protocol/build.gradle.kts:11-18`

```kotlin
implementation(
    project(
        mapOf(
            "path" to ":",
            "configuration" to "riderModel"
        )
    )
)
```

The `project(mapOf("path" to ":", "configuration" to "riderModel"))` syntax means: depend on the **root project**, but pull from its `riderModel` configuration specifically — not the default `runtimeElements` or `apiElements`. The map form is Gradle's typed-projection mechanism.

`implementation(...)` then puts that artifact (the `rider-model.jar` file) on the `:protocol` compile classpath. `Model.kt` can now `import com.jetbrains.rider.model.nova.ide.SolutionModel`.

## Why `builtBy` and not `dependsOn`?

`dependsOn` is task-to-task ordering. `builtBy` is artifact-level dependency ordering — "this artifact's existence depends on that task running first." When `:protocol`'s compile-classpath resolver asks "where's the JAR?", Gradle checks the `builtBy` declaration, sees IPGP's initialize task hasn't run, schedules it, and waits.

If you used `dependsOn` instead, you'd have to wire it onto every consumer task that resolves the configuration — much more fragile. The artifact-level pattern lets the data dependency drive the ordering automatically.

## Why this looks weird

A reader from a regular Gradle multi-project comes here knowing `implementation(project(":sub"))` and wonders what the configuration map is for. The answer: regular Gradle projects expose their `runtimeElements`/`apiElements` configurations by default, which contain the project's compiled JAR and the dependencies it needs at runtime. This use case isn't that — we're exposing a single file extracted from someone else's archive. So we declare a custom configuration and tell consumers to opt into it explicitly.

## Why this is canonical

This pattern shows up in many JetBrains-template-derived plugins:
- [`gradle-template`](https://github.com/JetBrains/rider-plugin-template)
- [`resharper-unity`](https://github.com/JetBrains/resharper-unity)
- Various JetBrains internal Rider plugins

If you've seen it once you'll recognize it. The "consumer-only configuration → publish a single SDK JAR → builtBy initialize" trio is standard for any Rider plugin that uses rdgen.

## What goes wrong if you break it

- Drop the `builtBy(...)`: random "rider-model.jar is not found" failures during clean builds. The `check { }` assertion catches it, but only with a vague error.
- Drop `isCanBeConsumed = true` (set both to `false`): nothing can pull from the configuration; `:protocol` fails to resolve.
- Drop `isCanBeResolved = false` and try to use the configuration locally: Gradle warns about a configuration that's both consumed-and-resolved (an anti-pattern called "legacy configurations").
- Try to do this without a configuration (e.g., directly add `files(...)` to `:protocol`'s classpath): you'd lose the artifact-level dependency ordering and have to add a `dependsOn` chain to every consumer.

## TL;DR mental model

> The root project owns the Rider SDK extraction. It exposes a typed pipe to the `:protocol` subproject saying "here's `rider-model.jar`, and don't try to read it before I've extracted the SDK." The `:protocol` subproject consumes from the typed pipe. That's all the `riderModel` configuration is.

→ Next: [14 · prepareSandbox — the glue](14-prepareSandbox-the-glue.md)
