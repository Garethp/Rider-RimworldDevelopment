# 01 · What is a JetBrains Rider plugin?

**[Foundation]**

Before you can read this build, you have to know what it's building. A Rider plugin is, physically, a `.zip` file with a specific directory layout that Rider unpacks into a plugins directory and loads at startup.

## The artifact

When `./gradlew buildPlugin` finishes, you get a file like `output/rimworlddev-2025.1.10.zip`. Inside it:

```
rimworlddev/
├── lib/
│   ├── rimworlddev.jar          ← compiled Kotlin (the "frontend" half)
│   └── (transitive JARs)
├── dotnet/                       ← THE BIT THAT'S UNIQUE TO RIDER
│   ├── ReSharperPlugin.RimworldDev.dll   (the "backend" half)
│   ├── ReSharperPlugin.RimworldDev.pdb
│   ├── 0Harmony.dll
│   ├── AsmResolver.dll
│   ├── (etc — runtime DLL deps)
└── ProjectTemplates/             ← templates for "New Rimworld Mod" project type
```

Rider, on startup, finds this folder and loads the JARs into its JVM and the DLLs into its .NET host. Both halves run, side by side, talking to each other.

## What's unique to Rider (vs. a regular IntelliJ plugin)

A standard IntelliJ plugin has only the `lib/` folder — JARs only. Rider plugins additionally have a `dotnet/` folder because **Rider is a dual-process IDE**: a JVM IDE on top, plus a separate .NET process underneath that handles all the C# language smarts. A Rider plugin frequently needs to extend both processes, so it ships a JVM half AND a .NET half, packaged together.

Everything else in this build system flows from that single fact. Most of the "weird" parts of `build.gradle.kts` exist because Gradle has to:
1. Build the JVM half (it knows how)
2. Build the .NET half (it does NOT know how natively — has to shell out to `dotnet build`)
3. Generate the wire-protocol code so the two halves can talk
4. Glue both halves into the right folders inside that ZIP
5. Optionally launch a Rider IDE against the result so you can poke at it

## One-paragraph mental model

> **This plugin has two halves. The "frontend" is JVM/Kotlin and runs inside Rider's UI process. The "backend" is .NET/C# and runs inside Rider's ReSharper-host process — the same process that already understands C# code, NuGet, MSBuild, etc. The two halves talk over Rider's RPC pipe (called "RD" — rdgen for the generator, RdFramework for the runtime). Gradle is the conductor: it builds the JVM half itself, shells out to `dotnet build` for the .NET half, runs an `:protocol` subproject to produce matching Kotlin and C# wire-protocol stubs from a single Kotlin model, then "prepares the sandbox" by laying out a fake plugin directory containing both halves' artifacts plus their dependencies, and finally either zips it (`buildPlugin`), launches a real Rider against it (`runIde`), or uploads it to the Marketplace (`publishPlugin`).**

That paragraph is the whole shape. Everything from here on is detail.

## Where this plugin's source lives

- **Frontend (Kotlin)**: `src/rider/main/kotlin/`, `src/rider/main/resources/META-INF/plugin.xml`
- **Backend (.NET/C#)**: `src/dotnet/ReSharperPlugin.RimworldDev/`
- **Protocol DSL (the wire format)**: `protocol/src/main/kotlin/model/rider/Model.kt`
- **Generated bindings (committed)**: `src/rider/main/kotlin/remodder/*.Generated.kt` and `src/dotnet/ReSharperPlugin.RimworldDev/*.Generated.cs`

The non-default `src/rider/...` and `src/dotnet/...` paths exist because the repo holds two languages side by side. They're explicitly wired in `build.gradle.kts:66-72`.

→ Next: [02 · The two-tier mental model](02-the-two-tier-mental-model.md)
