# 02 · The two-tier mental model

**[Foundation]**

Every confusing thing in this build is downstream of one fact: the running plugin spans two operating-system processes, each of which speaks a different language and has its own ecosystem. You have to internalize the boundary before the build's shape will make sense.

## The two processes

```
┌─────────────────────────────────────────────────────────────┐
│ Rider IDE process (JVM, Kotlin/Java)                         │
│                                                              │
│  - All UI: tool windows, dialogs, run config dialogs         │
│  - PSI for non-C# files (XML, JSON, etc.)                    │
│  - Run configurations (Run/Debug)                            │
│  - Settings UI                                               │
│  - Frontend half of this plugin: src/rider/main/kotlin/...   │
└──────────────────────────┬──────────────────────────────────┘
                           │
                  RD protocol over a pipe
                  (typed RPC, async + sync)
                           │
┌──────────────────────────┴──────────────────────────────────┐
│ Rider.Backend / ReSharper host process (.NET, C#)            │
│                                                              │
│  - C# language understanding (PSI, types, references)        │
│  - ReSharper analyzers, completions, quick-fixes             │
│  - MSBuild project model, NuGet awareness                    │
│  - Decompiler                                                │
│  - Backend half of this plugin: src/dotnet/...               │
└─────────────────────────────────────────────────────────────┘
```

When you see a UI (e.g. a tool window listing transpiled methods), the click flows through the JVM process. When you ask "where is this `def` defined?" by Ctrl-clicking inside Rimworld XML, the actual lookup runs in the .NET process — because it's the .NET process that has the C# type system loaded and can read `Assembly-CSharp.dll`.

## Where does new code go?

A decision matrix you'll consult often:

| New feature | Where | Why |
|---|---|---|
| Tool window, dialog, settings page | Frontend (Kotlin) | UI is JVM-only |
| Run configuration | Frontend (Kotlin) | Run configs are an IntelliJ Platform concept |
| XML completion items derived from Rimworld's `Assembly-CSharp` | Backend (C#) | Needs the C# type system |
| ReSharper analyzer or quick-fix | Backend (C#) | Analyzers are a ReSharper concept |
| Decompilation, IL inspection | Backend (C#) | Uses .NET libraries (AsmResolver, ICSharpCode.Decompiler) |
| Anything that needs to call across | Both, plus an RD endpoint | The protocol is how they communicate |

## How the halves talk

JetBrains' answer is **RD (Reactive Distributed)** — a typed RPC system. You write a single Kotlin file (`protocol/src/main/kotlin/model/rider/Model.kt`) that declares calls, signals, and properties. A Gradle task (`:protocol:rdgen`) reads that and emits **two** files: a Kotlin one for the frontend and a C# one for the backend. Both files describe the same wire format, in their respective languages. At runtime the two sides bind to a shared pipe and method invocations are marshalled across.

This plugin currently has exactly one RPC: `decompile(string[]) -> string[]`, defined at `protocol/src/main/kotlin/model/rider/Model.kt:16`. The frontend's Transpilation Explorer tool window calls it; the backend uses ICSharpCode.Decompiler to do the work.

## Why the build feels weird because of this

The build system is, at heart, a JVM build (Gradle) that has to:

1. Build the .NET half by shelling out to `dotnet`
2. Coordinate a code generator (rdgen) that targets two languages
3. Stitch JARs and DLLs into a single ZIP with a layout the IDE expects
4. Launch a JVM IDE that itself launches a .NET process — both of which need to find the plugin's bits

Gradle has no native support for any of this. The IntelliJ Platform Gradle Plugin (IPGP) handles a lot. The rest is custom glue inside `build.gradle.kts`. The "weird" tasks (`compileDotNet`, `prepareSandbox`'s manual DLL list, the `riderModel` configuration, the DotFiles patcher) all exist to plaster over this gap.

→ Next: [03 · Gradle 101 — just enough](03-gradle-101-just-enough.md)
