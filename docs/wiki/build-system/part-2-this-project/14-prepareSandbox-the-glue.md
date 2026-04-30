# 14 · prepareSandbox — the glue

**[This Project]** — *The most fragile and most load-bearing block in `build.gradle.kts`. Read this BEFORE you change anything in the file.*

`prepareSandbox` is the task that physically lays out the plugin's files into a fake plugins directory the way Rider expects them. It's contributed by IPGP, configured in this repo at `build.gradle.kts:156-224`. The block does **two distinct things in one place**: declarative file copying (Gradle's Copy machinery) and imperative post-actions (existence checks + DotFiles patching).

## What "the sandbox" is

Run `./gradlew runIde` and IPGP creates `build/idea-sandbox/` containing:

```
build/idea-sandbox/
├── config/        ← IDE settings (config dir)
├── system/        ← caches, indexes
└── plugins/
    └── rimworlddev/                        ← THIS IS WHAT prepareSandbox FILLS IN
        ├── lib/
        │   └── rimworlddev.jar             ← compiled Kotlin
        ├── dotnet/                          ← THE LOAD-BEARING CONVENTION
        │   ├── ReSharperPlugin.RimworldDev.dll
        │   ├── ReSharperPlugin.RimworldDev.pdb
        │   ├── 0Harmony.dll
        │   ├── AsmResolver.dll
        │   └── (more)
        └── ProjectTemplates/
            └── RimworldProjectTemplate/
                └── ...
```

When the launched Rider boots, it discovers `build/idea-sandbox/plugins/rimworlddev/` and treats it as an installed plugin. The frontend JARs go into the JVM. The DLLs in `dotnet/` get pushed into the backend's plugin scope.

The `dotnet/` folder name is **a load-bearing convention** — Rider's plugin loader sweeps `<plugin-root>/dotnet/*.dll` automatically. That's why `prepareSandbox` copies into `${rootProject.name}/dotnet` (`build.gradle.kts:175`).

## The block, walked

`build.gradle.kts:156-224`. Three parts.

### Part 1: ordering and inputs (`:157-178`)

```kotlin
tasks.prepareSandbox {
    dependsOn(compileDotNet)

    val outputFolder = "${rootDir}/src/dotnet/${DotnetPluginId}/bin/${DotnetPluginId}.Rider/${BuildConfiguration}"
    val dllFiles = listOf(
        "$outputFolder/${DotnetPluginId}.dll",
        "$outputFolder/${DotnetPluginId}.pdb",
        // Not 100% sure why, but we manually need to include these dependencies for Remodder to work
        "$outputFolder/0Harmony.dll",
        "$outputFolder/AsmResolver.dll",
        "$outputFolder/AsmResolver.DotNet.dll",
        "$outputFolder/AsmResolver.PE.dll",
        "$outputFolder/AsmResolver.PE.File.dll",
        "$outputFolder/ICSharpCode.Decompiler.dll"
    )

    dllFiles.forEach({ f ->
        val file = file(f)
        from(file, { into("${rootProject.name}/dotnet") })
    })

    from("${rootDir}/src/dotnet/${DotnetPluginId}/ProjectTemplates", { into("${rootProject.name}/ProjectTemplates") })
```

This part:
- `dependsOn(compileDotNet)` — ensure `dotnet build` ran before we try to copy the DLLs
- Reads from `bin/ReSharperPlugin.RimworldDev.Rider/Release/` (the Rider flavour; see §12)
- Hand-lists 8 files: the main DLL+PDB plus 6 Remodder runtime dependencies
- `from(file, { into("..../dotnet") })` — Gradle Copy DSL; declares input files and a destination subpath inside the sandbox
- Also copies the `ProjectTemplates/` folder

**The hand-listed DLLs are a workaround.** IPGP's default sandbox layout knows about the main plugin DLL but not about transitive runtime dependencies. The .NET project sets `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>` (in `.Rider.csproj:18`) to dump those DLLs into `bin/`, then this Gradle list re-copies them by name into the sandbox.

The comment in the code is honest:
> *"Not 100% sure why, but we manually need to include these dependencies for Remodder to work"*

The "why" is what's described above: the bin/ folder has the DLLs (because of the .csproj flag) but `prepareSandbox` doesn't pick them up automatically (because the Copy DSL only includes what `from(...)` lines explicitly list).

**Footgun:** if you add a new NuGet package to `.Rider.csproj` whose runtime DLL needs to be in the plugin (rather than provided by Rider), you must add the DLL filename here too. Otherwise the build succeeds but the plugin fails at runtime with a `FileNotFoundException` for the missing assembly.

### Part 2: existence assertion (`:180-184`)

```kotlin
doLast {
    dllFiles.forEach({ f ->
        val file = file(f)
        if (!file.exists()) throw RuntimeException("File ${file} does not exist")
    })
    // ... continued in part 3
```

A runtime sanity check. If `compileDotNet` succeeded but didn't actually produce one of the expected DLLs (for example, you renamed an excluded source file but forgot to update the exclude rule), the build fails here with a clear message.

This is technically duplicate work — Gradle's Copy task would itself fail if a `from(file)` source didn't exist. But the explicit check produces a clearer error message, and the failure mode "Configuration changed silently and we now ship a broken plugin" is high-stakes enough to justify defense in depth.

### Part 3: the DotFiles workaround (`:186-222`)

The hairiest piece in the build. The comment in the code:

> *"The Rider SDK archive omits certain DLLs that are present in a full Rider installation. Copy the missing Unity plugin DotFiles DLL from the local Rider installation so the sandbox can load it."*

What's actually happening:

The plugin declares `<depends>com.intellij.resharper.unity</depends>` (`plugin.xml:8`). Rider's Unity plugin ships with native helper DLLs in `plugins/rider-unity/DotFiles/`. These are PE-format binaries the Unity debugger injects into running Unity processes.

- On **Windows**, Rider's Maven artifact (the `rider:2026.1` distribution IPGP downloads) ships these DLLs.
- On **Mac/Linux**, the Maven artifact is **stripped** of them (presumably to keep platform-specific natives out of a generic JAR). But local Rider installations have them — they're put there by the DMG/tarball installer.

Without these DLLs, the rider-unity plugin fails to initialize on Mac/Linux when the sandbox boots. And because this plugin `<depends>` on rider-unity, our plugin transitively fails to load.

The hack copies them from a local Rider installation into the sandbox's SDK extraction:

```kotlin
if (!isWindows) {
    val riderInstallCandidates = if (Os.isFamily(Os.FAMILY_MAC)) {
        listOf(file("/Applications/Rider.app/Contents"))
    } else {
        // Linux: check JetBrains Toolbox and common standalone install paths
        val toolboxBase = file("${System.getProperty("user.home")}/.local/share/JetBrains/Toolbox/apps/Rider")
        val toolboxInstalls = if (toolboxBase.exists()) {
            toolboxBase.walkTopDown()
                .filter { it.name == "plugins" && it.parentFile?.name?.startsWith("2") == true }
                .map { it.parentFile }
                .toList()
        } else emptyList()
        toolboxInstalls + listOf(file("/opt/rider"), file("/usr/share/rider"))
    }

    val missingDotFileDlls = listOf(
        "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.PausePoint.Helper.dll",
        "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture.dll",
    )

    val destDir = intellijPlatform.platformPath.resolve("plugins/rider-unity/DotFiles").toFile()
    destDir.mkdirs()

    for (dllName in missingDotFileDlls) {
        val dllRelPath = "plugins/rider-unity/DotFiles/$dllName"
        val srcDll = riderInstallCandidates
            .map { file("${it}/${dllRelPath}") }
            .firstOrNull { it.exists() }

        if (srcDll != null) {
            srcDll.copyTo(file("${destDir}/${srcDll.name}"), overwrite = true)
        }
    }
}
```

Three things to call out:

1. **The destination is the sandbox's *SDK* directory**, not the plugin's own staging. `intellijPlatform.platformPath.resolve("plugins/rider-unity/DotFiles")` writes into where the bundled Unity plugin reads from at runtime. We're patching the SDK in place.

2. **It silently no-ops if no candidate path matches.** Run `runIde` on a Linux machine without Rider installed and you get no warning, just a sandbox where Unity debugging silently doesn't work.

3. **It's execution-time only.** All the file walking happens inside `doLast`. Pulling it to configuration time would crash because `intellijPlatform.platformPath` isn't valid until IPGP's initialize task has run.

**Why this is a "Help wanted from JetBrains" workaround:** the right fix is for JetBrains to either ship those DLLs in the cross-platform Maven artifact, or to have IPGP fetch them on-demand. Until then, the wiki marks this as needing JetBrains involvement (§17, §23).

## Mental model

```
                       compileDotNet
                            │
                            ▼
              src/dotnet/.../bin/ReSharperPlugin.RimworldDev.Rider/Release/
                            │
                  prepareSandbox copies named DLLs
                            │
                            ▼
          build/idea-sandbox/plugins/rimworlddev/dotnet/
                            │
                  Rider plugin loader picks up dotnet/*.dll
                            │
                            ▼
                  Backend (Rider.Backend) loads them

[On Mac/Linux only]
       Local Rider install plugins/rider-unity/DotFiles/*.dll
                            │
              prepareSandbox copies into platformPath
                            │
                            ▼
        build/idea-sandbox/.../platform/plugins/rider-unity/DotFiles/*.dll
                            │
                  Bundled rider-unity plugin loads them
```

## What to do when this breaks

| Symptom | Likely cause | Fix |
|---|---|---|
| `File .../ReSharperPlugin.RimworldDev.dll does not exist` | `compileDotNet` failed silently or didn't run | `./gradlew compileDotNet --info` and read .NET errors |
| `File .../0Harmony.dll does not exist` | NuGet didn't restore, or `<CopyLocalLockFileAssemblies>` got removed | `dotnet restore`; check `.Rider.csproj:18` |
| `runIde` boots but the plugin fails to load on Linux/Mac | Unity DotFiles missing; install Rider locally via Toolbox | Or, in CI: this currently doesn't bite because CI doesn't run `runIde` |
| New NuGet runtime DLL needed but not bundled | The `dllFiles` list at `build.gradle.kts:160-171` doesn't include it | Add the DLL filename to the list |

→ Next: [15 · runIde and debugging locally](15-runIde-and-debugging-locally.md)
