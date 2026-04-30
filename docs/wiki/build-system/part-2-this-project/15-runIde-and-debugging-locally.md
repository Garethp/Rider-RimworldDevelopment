# 15 · runIde and debugging locally

**[This Project]** — *Mostly the same as IntelliJ plugins, with one twist: you have a second process to attach to.*

`runIde` is the developer-feedback loop. It builds the plugin, lays out a sandbox (§14), and launches a sandboxed Rider with the plugin pre-installed and a known mod project pre-opened. This page covers using it, attaching debuggers, and reading logs.

## The basic loop

```bash
./gradlew runIde
```

What happens, in dependency order:

1. IPGP initialize task — downloads & extracts the Rider SDK on first run (~10 GB; cached afterwards)
2. `compileKotlin` — JVM frontend
3. `compileDotNet` — `dotnet build` for the .NET backend
4. `prepareSandbox` — copies DLLs + JARs + ProjectTemplates into `build/idea-sandbox/plugins/rimworlddev/`
5. `runIde` — launches Rider against the sandbox, pointed at `example-mod/AshAndDust.sln`

A clean first run takes 5–15 minutes (mostly downloading Rider). Subsequent runs are usually 30–60 seconds — but `compileDotNet` always re-runs (§09 §17), so even no-op cycles aren't instant.

## What's configured

`build.gradle.kts:141-154`:

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

- `maxHeapSize = "1500m"` — matches Rider's default. IPGP's default of 512m chokes on real solutions.
- `autoReload = false` — Rider's backend doesn't support dynamic plugin reload. The .NET process can't safely swap code mid-flight, and a desync between the JVM frontend (which can reload) and the .NET backend (which can't) is worse than no reload. Disabled by design.
- The `argumentProviders` lambda passes `example-mod/AshAndDust.sln` to the launched Rider as a CLI argument. Result: Rider auto-opens the example mod's solution, so you have something to test against immediately.

## Skipping .NET if you only changed Kotlin

```bash
./gradlew runIde -x compileDotNet
```

Half the iteration time. The `-x` flag tells Gradle to exclude a task. Useful when you're only iterating on the JVM half (UI, run configurations, settings).

## Prerequisites

For a successful first-time `runIde`:

- **JDK 21** — Gradle's toolchain auto-provisioning will fetch one if not present, but it's faster to install ahead.
- **.NET SDK 7.0+ on PATH** — `dotnet` must work from your terminal (`global.json` pins 7.0.202 with `rollForward: latestMajor`).
- **~10 GB free disk** — Rider SDK download + sandbox + outputs.
- **Internet on first run** — for the SDK download.

On **Linux/macOS** additionally: install Rider locally via JetBrains Toolbox (or `/opt/rider`, `/usr/share/rider`, `/Applications/Rider.app`). The `prepareSandbox` DotFiles patcher (§14) copies Unity-debugger DLLs from your local install into the sandbox. Without those DLLs, the bundled rider-unity plugin fails to initialize and our plugin (which `<depends>` on it) won't load. Currently no warning is emitted — silent footgun.

## The two debugger stories

The plugin runs in two processes, so debugging means choosing which.

### Frontend (JVM/Kotlin)

`runIde` runs the launched Rider as a child JVM. Gradle sets up debugging by default — IntelliJ-family IDEs running `runIde` from a Gradle task config provide a "Debug" green-bug button next to the Run button. Click it, breakpoints in `src/rider/main/kotlin/...` work as expected.

If running `./gradlew runIde` from a terminal, you can attach manually: the JVM is launched with `-agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=*:<port>` style args (controlled by IPGP). Find the port in IDE log output, then "Attach to JVM Process" in your dev IDE.

### Backend (.NET/C#)

The launched Rider spawns a separate `Rider.Backend` (or `dotnet/dotnet`) process. To debug the .NET half:

1. Run `./gradlew runIde` and let the sandbox boot fully.
2. In your **development** Rider (the one you opened the plugin source in), choose *Run → Attach to Process…*
3. Find the `Rider.Backend.exe` (Windows) or `dotnet` (Linux/macOS) process spawned by the sandbox Rider. There may be multiple `dotnet` processes; pick the one with `Rider` in the command line.
4. Attach. Set breakpoints in `src/dotnet/...`.
5. The PDB files copied into the sandbox by `prepareSandbox` provide source mappings.

This is the same dance as debugging any out-of-process .NET application. There's no Gradle integration for it — JetBrains does not currently provide one.

## Reading logs

Sandbox logs live under `build/idea-sandbox/`:

- **JVM frontend (idea.log)**: `build/idea-sandbox/system/log/idea.log`. The standard IntelliJ Platform log file. Plugin Kotlin code's `Logger.getInstance(...).warn(...)` ends up here.
- **.NET backend (Rider.Backend logs)**: `build/idea-sandbox/system/log/Rider.Backend/log/`. Backend C# logs land here. JetBrains' RD logs and ReSharper plugin logs are mixed in.

For a fast `tail`:

```bash
tail -f build/idea-sandbox/system/log/idea.log
```

```bash
ls build/idea-sandbox/system/log/Rider.Backend/log/
# follow the most recent file
```

## Common failure modes

| Symptom | Where to look |
|---|---|
| `runIde` hangs at "Building" | Probably .NET restore. `./gradlew compileDotNet --info` to see what's stuck. |
| Sandbox boots but plugin not loaded | Read `idea.log` for plugin loader errors. Common cause: `<depends>` on a plugin/module that's not bundled (e.g. a renamed `intellij.spellchecker`). |
| RD protocol calls hang on the frontend | Backend probably didn't bind. Check the Rider.Backend log for serialization-hash mismatches (means RdFramework versions don't agree). |
| "DotFiles not found" message during sandbox boot | You're on Mac/Linux and don't have Rider installed locally. Install via Toolbox. |
| Plugin loads but XML completion does nothing | `prepareSandbox` may have missed a DLL — check `<plugin>/dotnet/` in the sandbox tree to confirm all expected DLLs are there. |

## Useful Gradle invocations

```bash
./gradlew runIde --dry-run                  # show task graph without running
./gradlew runIde -i                          # info-level logging
./gradlew runIde --warning-mode all          # surface every deprecation
./gradlew runIde --configuration-cache       # try with config cache (will surface cache hostility)
./gradlew :buildPlugin                       # just produce the ZIP
./gradlew clean                              # nuke build/ entirely
./gradlew :protocol:rdgen                    # regenerate protocol bindings
```

→ Next: [16 · CI and publishing](16-ci-and-publishing.md)
