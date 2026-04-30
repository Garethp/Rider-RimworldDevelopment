# 21 · Contributed tasks table

**[Reference]** — *Every Gradle task this build exposes, who contributes it, what it does, and whether it's incremental.*

When you see `tasks.somethingYouNeverDefined { ... }` in `build.gradle.kts`, this table tells you who added it.

## IPGP-contributed (IntelliJ Platform Gradle Plugin)

The bulk of the build's "it just works" surface area.

| Task | Purpose | Inputs/Outputs | Incremental? |
|---|---|---|---|
| `initializeIntelliJPlatformPlugin` | Download + extract the Rider SDK | network → `~/.gradle/caches/intellij-platform/...` | Yes (cached) |
| `setupDependencies` | Wire IDE deps onto compile classpath | (transparent) | Yes |
| `prepareSandbox` | Lay out plugin files into `build/idea-sandbox/plugins/<name>/` | `from(...)` files → sandbox dir | Yes (Copy task) but `doLast` actions are not |
| `runIde` | Launch sandboxed Rider | sandbox + JBR | N/A (always runs) |
| `buildPlugin` | Zip the sandbox into a distributable plugin ZIP | sandbox → `build/distributions/<name>-<version>.zip` | Yes |
| `verifyPlugin` | Run JetBrains' Plugin Verifier across IDE versions | plugin ZIP + IDE versions → verifier reports | Yes |
| `publishPlugin` | Upload plugin ZIP to JetBrains Marketplace | plugin ZIP + token → Marketplace | N/A (network) |
| `patchPluginXml` | Rewrite `plugin.xml` with version, change notes, build range | plugin.xml → patched plugin.xml | Yes |
| `instrumentCode` | NotNull annotation bytecode instrumentation | classes → instrumented classes | **DISABLED** in this build (`build.gradle.kts:74-76`) |
| `instrumentTestCode` | Same, for test classes | (transparent) | **DISABLED** in this build (`:78-80`) |
| `printBundledPlugins` | Diagnostic: list bundled plugins available in the configured SDK | (transparent) | N/A |
| `printProductsReleases` | Diagnostic: list known IDE releases | (transparent) | N/A |

Configured in `build.gradle.kts`: `runIde` (`:141-154`), `prepareSandbox` (`:156-224`), `buildPlugin` (`:107-114`), `patchPluginXml` (`:238-252`), `publishPlugin` (`:232-236`), `pluginVerification` extension (`:130-139`).

## Custom (defined in this build)

| Task | Type | Purpose | Inputs/Outputs declared? |
|---|---|---|---|
| `compileDotNet` | `Exec` | `dotnet build --configuration Release` | **No** — re-runs every build |
| `buildResharperPlugin` | `Exec` | `dotnet msbuild $sln /t:Restore;Rebuild;Pack` → produces `.nupkg` | **No** |
| `testDotNet` | `Exec` | `dotnet test $sln --logger GitHubActions` | **No** |

All three live in `build.gradle.kts:86-105, 226-230`. Captured as refactor candidates in §24 (convert to typed task class with proper `@InputDirectory`/`@OutputDirectory`).

## Kotlin Gradle plugin

| Task | Purpose |
|---|---|
| `compileKotlin` | Compile `src/rider/main/kotlin/` to JVM bytecode |
| `compileTestKotlin` | Compile Kotlin tests (none in this project) |
| `kotlinDslAccessorsReport` | DSL accessors diagnostic |

Configured at `build.gradle.kts:82-84` (sets `JvmTarget.JVM_21`).

## `:protocol` subproject

| Task | Type | Purpose |
|---|---|---|
| `:protocol:rdgen` | `RdGenTask` | Run rdgen — generates `*.Generated.kt` and `*.Generated.cs` from `Model.kt` |
| `:protocol:compileKotlin` | (Kotlin plugin) | Compile the protocol DSL itself |

Configured in `protocol/build.gradle.kts:24-46`. Note rdgen output files are committed; the task is only run manually (`./gradlew :protocol:rdgen`).

## Java plugin (built-in)

| Task | Purpose |
|---|---|
| `compileJava` | Compile `.java` sources (this repo has none, but the dir is declared) |
| `processResources` | Copy resources to `build/resources/main/` |
| `classes` | Aggregate of compileKotlin + compileJava + processResources |
| `jar` | Produce `build/libs/<project>-<version>.jar` |
| `wrapper` | Regenerate `gradlew` / `gradle/wrapper/gradle-wrapper.properties` |

Configured at `build.gradle.kts:14-20` (Java toolchain), `:57-62` (`tasks.wrapper`).

## How to discover tasks yourself

```bash
./gradlew tasks                 # all tasks (grouped by plugin)
./gradlew tasks --all           # including hidden ones
./gradlew help --task <name>    # detail on one task
./gradlew runIde --dry-run      # show task graph without running
./gradlew :protocol:tasks       # tasks specific to subproject
```

For task implementation classes, look at the IDE inspection on `tasks.<name> {` in your dev IDE — it'll resolve to the contributed plugin's class.

→ Next: [22 · Glossary](22-glossary.md)
