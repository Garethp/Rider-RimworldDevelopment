# 10 · The protocol subproject and rdgen

**[This Project]** — *This is unique to Rider plugins. rdgen / RD doesn't exist in standard IntelliJ plugins.*

The `:protocol` subproject is small but conceptually dense. Its only job is to run **rdgen**, JetBrains' protocol code generator, which turns a single Kotlin DSL file into matched Kotlin and C# source files. Those generated files are how the JVM frontend and .NET backend speak to each other.

## What rdgen does

You write one Kotlin file describing the protocol — calls, signals, properties, structs. rdgen reads it and produces:

- A **Kotlin** file the frontend imports
- A **C#** file the backend imports

Both files describe the same wire format in their respective languages. At runtime, the RD framework on each side binds to a shared pipe and marshals method invocations across.

```
                  protocol/src/main/kotlin/model/rider/Model.kt
                                    │
                              :protocol:rdgen
                                    │
                       ┌────────────┴────────────┐
                       ▼                         ▼
         RemodderProtocolModel.Generated.kt   RemodderProtocolModel.Generated.cs
         (JVM frontend uses this)              (.NET backend uses this)
```

## The model file

`protocol/src/main/kotlin/model/rider/Model.kt`:

```kotlin
package model.rider

import com.jetbrains.rd.generator.nova.Ext
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator
import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rider.model.nova.ide.SolutionModel

object RemodderProtocolModel : Ext(SolutionModel.Solution) {
    init {
        setting(CSharp50Generator.Namespace, "ReSharperPlugin.RdProtocol")
        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.plugins.rdprotocol")

        // Remote procedure on backend
        call("decompile", array(string), array(string)).async
    }
}
```

What's happening:

- `object RemodderProtocolModel : Ext(SolutionModel.Solution)` — declares an *extension* of the Rider **Solution-scoped** protocol. "Ext" means "this protocol attaches to a host scope"; the host scope here is `SolutionModel.Solution`, so the protocol exists per open solution. Other scopes you'd extend in different plugins: `IdeRoot` (application-wide), `RdSolution` (alternative solution scope), etc.
- `setting(...Namespace, "...")` — output namespace for each language
- `call("decompile", array(string), array(string)).async` — declares an asynchronous RPC named `decompile` taking `string[]` and returning `string[]`

That's literally all this plugin's protocol does today: one async call.

## The :protocol build script

`protocol/build.gradle.kts`:

```kotlin
plugins {
    id("org.jetbrains.kotlin.jvm")
    id("com.jetbrains.rdgen") version libs.versions.rdGen
}

dependencies {
    implementation(libs.kotlinStdLib)
    implementation(libs.rdGen)
    implementation(
        project(
            mapOf(
                "path" to ":",
                "configuration" to "riderModel"
            )
        )
    )
}

val DotnetPluginId: String by rootProject
val RiderPluginId: String by rootProject

rdgen {
    val csOutput = File(rootDir, "src/dotnet/${DotnetPluginId}")
    val ktOutput = File(rootDir, "src/rider/main/kotlin/remodder")
    verbose = true
    packages = "model.rider"

    generator {
        language = "kotlin"
        transform = "asis"
        root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
        namespace = "com.jetbrains.rider.model"
        directory = "$ktOutput"
    }

    generator {
        language = "csharp"
        transform = "reversed"
        root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
        namespace = "JetBrains.Rider.Model"
        directory = "$csOutput"
    }
}

tasks.withType<RdGenTask> {
    val classPath = sourceSets["main"].runtimeClasspath
    dependsOn(classPath)
    classpath(classPath)
}
```

Walk-through of the non-obvious parts:

### `version libs.versions.rdGen` (`:5`)

`libs.versions.rdGen` is a `Provider<String>`. The `version` keyword in the plugin DSL accepts both `String` and `Provider<String>`, so this works. Resolves to `2026.1.3` from `gradle/libs.versions.toml:3`.

### `implementation(project(mapOf("path" to ":", "configuration" to "riderModel")))` (`:11-18`)

Most subprojects depend on each other with `implementation(project(":root"))`. Here we're saying: depend on the root project, but specifically on its `riderModel` configuration — not the default `runtimeElements` or `apiElements`.

That `riderModel` configuration is declared in the root `build.gradle.kts:254-269` and exposes `lib/rd/rider-model.jar` from inside the extracted Rider SDK. The `:protocol` module needs that JAR on its classpath because `Model.kt:8` extends `SolutionModel.Solution` defined inside it. See §13 for the full bridge mechanics.

### Two `generator { }` blocks (`:31-45`)

One per output language. Notable settings:

- `language = "kotlin" | "csharp"` — picks the codegen backend
- `transform = "asis" | "reversed"` — the **call direction**. `"asis"` keeps the original direction of declared calls; `"reversed"` flips it. Why: a `call("decompile", ...)` in the model is, semantically, "the frontend asks the backend to decompile". The Kotlin output (consumed by frontend) wants `decompile.start(...)` for *initiating* the call (asis); the C# output (consumed by backend) wants `decompile.SetAsync(...)` for *handling* it (reversed). The two transforms produce mirror-image bindings.
- `root = "com.jetbrains.rider.model.nova.ide.IdeRoot"` — anchor type for code generation, defined in `rider-model.jar`
- `namespace` — package/namespace of the generated *root model*. The protocol-specific namespace was set inside `Model.kt:12-13`
- `directory` — where to drop the `.kt` / `.cs` files

### `tasks.withType<RdGenTask> { dependsOn(classPath); classpath(classPath) }` (`:48-52`)

`RdGenTask` is a Gradle task type provided by the rdgen plugin. We need to wire **the `:protocol` module's own compiled Kotlin classes** onto the rdgen task's classpath, because rdgen reflects on those classes (the `RemodderProtocolModel` object) at run time to discover the model.

`tasks.withType<RdGenTask> { ... }` configures all tasks of that type lazily — including any added later. Gradle pattern: when a plugin adds tasks of a type, configure them this way to be future-proof.

## Why the generated files are committed

Both `RemodderProtocolModel.Generated.kt` (`src/rider/main/kotlin/remodder/`) and `RemodderProtocolModel.Generated.cs` (`src/dotnet/ReSharperPlugin.RimworldDev/`) live in git.

**This is intentional.** Reasons:

1. **rdgen is not on the build path of `:buildPlugin` / `:compileKotlin`.** It only runs when you explicitly invoke `./gradlew :protocol:rdgen`. So a contributor who hasn't run it can still build the plugin.
2. **Auditable PR diffs.** When the protocol changes, the diff is visible in code review — both halves' bindings are part of the patch.
3. **No hard dependency on the rdgen toolchain at build time.** A contributor doesn't need a working rdgen environment just to compile the plugin.

The trade-off: you must remember to re-run `./gradlew :protocol:rdgen` after editing `Model.kt` and commit the regenerated files. CI does not regenerate; it consumes what's checked in.

## How to add a new RPC

1. Edit `protocol/src/main/kotlin/model/rider/Model.kt`. Add a `call(...)`, `signal(...)`, or `property(...)` inside `init { }`.
2. Run `./gradlew :protocol:rdgen`.
3. The regenerated `*.Generated.kt` and `*.Generated.cs` will appear with new symbols.
4. Implement the call site (frontend) and handler (backend).
5. Commit the regenerated files alongside your protocol edit.

## Threading and the `.async` modifier

The current `decompile` call has `.async` appended:

```kotlin
call("decompile", array(string), array(string)).async
```

`.async` means: the handler can complete the call later (return a `Task<T[]>` / `Promise<T[]>`); the framework won't block waiting. Without `.async`, the call is synchronous and the handler must produce the result on the same thread it was invoked on.

On the Kotlin side, the generated `RemodderProtocolModel.Generated.kt` registers `extThreading` for the call. On the C# side, the handler is registered via `model.Decompile.SetAsync(...)` and runs under a `ReadLockCookie.Create()`-scoped operation in `RemodderComponent.cs`.

You don't need to remember the details. The pattern is: **declarative threading in the model, mechanical bindings on both sides, `.async` flag for non-blocking calls.**

→ Next: [11 · The .NET side](11-the-dotnet-side.md)
