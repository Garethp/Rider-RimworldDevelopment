# 06 · Repo tour

**[This Project]** — *This is mostly the same as IntelliJ plugins, with a `src/dotnet/` tree added on the side.*

A guided walk through the directory tree, top down. What's in each top-level entry, and why.

```
Rider-RimworldDevelopment/
├── build.gradle.kts                  ← root Gradle build script
├── settings.gradle.kts               ← Gradle settings (subprojects, pluginManagement)
├── gradle.properties                 ← simple key=value config
├── gradle/
│   ├── libs.versions.toml            ← version catalog (Kotlin, rdGen)
│   └── wrapper/                      ← Gradle wrapper (pinned 9.4.1)
├── gradlew, gradlew.bat              ← Gradle wrapper launcher
├── global.json                       ← .NET SDK pin (7.0.202)
├── Directory.Build.props             ← MSBuild props for every .csproj
├── ReSharperPlugin.RimworldDev.sln   ← .NET solution containing the 3 csprojs
│
├── protocol/                         ← :protocol subproject
│   ├── build.gradle.kts              ← rdgen wiring
│   └── src/main/kotlin/model/rider/
│       └── Model.kt                  ← THE protocol DSL (§10)
│
├── src/
│   ├── rider/main/                   ← JVM/Kotlin frontend
│   │   ├── kotlin/                   ← Kotlin sources
│   │   │   └── remodder/
│   │   │       └── RemodderProtocolModel.Generated.kt   ← rdgen output, COMMITTED
│   │   └── resources/
│   │       └── META-INF/plugin.xml   ← Rider plugin descriptor
│   │
│   └── dotnet/
│       ├── ReSharperPlugin.RimworldDev/
│       │   ├── ReSharperPlugin.RimworldDev.csproj        ← Wave/ReSharper flavor
│       │   ├── ReSharperPlugin.RimworldDev.Rider.csproj  ← Rider flavor
│       │   ├── *.cs                                      ← shared C# sources
│       │   ├── RemodderProtocolModel.Generated.cs        ← rdgen output, COMMITTED
│       │   ├── ProjectTemplates/                         ← New-Mod templates
│       │   ├── Remodder/                                 ← decompilation feature (Rider-only)
│       │   ├── References/                               ← XML→C# navigation
│       │   ├── ItemCompletion/                           ← XML autocompletion
│       │   ├── ProblemAnalyzers/                         ← validation
│       │   ├── RimworldXmlProject/                       ← custom project type
│       │   └── (other features)
│       └── ReSharperPlugin.RimworldDev.Tests/
│           └── ReSharperPlugin.RimworldDev.Tests.csproj  ← stub: zero .cs test files yet
│
├── example-mod/                      ← real Rimworld mod, opened by runIde
│   ├── AshAndDust.sln
│   ├── Source/
│   ├── About/, Defs/, Patches/, Languages/, Textures/...
│   └── 1.4/, 1.5/, ...               ← multi-version support folders
│
├── .github/workflows/
│   ├── CI.yml                        ← push/PR build & test
│   └── Deploy.yml                    ← tag-triggered publish
│
├── .run/                             ← IntelliJ run configurations (some stale)
│   ├── Build Plugin.run.xml
│   └── Build ReSharper Plugin.run.xml
│
├── runVisualStudio.ps1               ← legitimate (legacy ReSharper-for-VS dev)
├── buildPlugin.ps1                   ← LEGACY, not used by CI
├── publishPlugin.ps1                 ← LEGACY, not used by CI
├── settings.ps1                      ← LEGACY (vswhere wrapper)
├── tools/
│   ├── vswhere.exe                   ← LEGACY
│   └── nuget.exe                     ← LEGACY
│
├── CHANGELOG.md                      ← parsed at build time by patchPluginXml
├── README.md
└── output/                           ← build artifacts land here (gitignored)
    └── rimworlddev-X.Y.Z.zip         ← the final Rider plugin distribution
```

## Notable conventions

- **`src/rider/...` instead of `src/main/...`**: explicitly wired in `build.gradle.kts:66-72`. The repo holds two languages, so they're segregated under `rider/` (JVM) and `dotnet/` (.NET). A reader from a single-language Gradle project will hit this immediately and wonder why.
- **Generated files are committed**: both `RemodderProtocolModel.Generated.kt` and `RemodderProtocolModel.Generated.cs`. They're rdgen output. We commit them on purpose — see §10.
- **The .NET solution sits at the repo root** (`ReSharperPlugin.RimworldDev.sln`) but the projects live under `src/dotnet/`. That's an MSBuild convention quirk; the solution file references projects by relative path.
- **Three `.csproj` files share the source tree** at `src/dotnet/ReSharperPlugin.RimworldDev/`. They produce different assemblies via different `<DefineConstants>` and `<Compile Remove>` rules. See §12.
- **`example-mod/` is a real mod, not a synthetic test fixture** — passed to `runIde` as an argument so the launched Rider opens it. Currently a manual fixture; future basis for integration tests (§17).
- **`output/`** is where `tasks.buildPlugin { doLast { copy(...) } }` (`build.gradle.kts:107-114`) places the final ZIP. CI's GitHub Release upload reads from here, which is why the copy exists.

## Files most likely to drift / mislead

These are tagged in §17 with details:

| File | Issue |
|---|---|
| `gradle.properties:26-28` vs `build.gradle.kts:9-10` | Same plugin versions, drifted (`gradleJvmWrapperVersion=0.15.0` vs `0.16.0`) |
| `gradle.properties:28` `riderBaseVersion` | Dead — zero references |
| `.run/Build Plugin.run.xml:8` | References `corretto-17.0.7` but toolchain is JDK 21 |
| `.run/Build ReSharper Plugin.run.xml` | Invokes legacy `buildPlugin.ps1` |
| `*.ps1` (root, except `runVisualStudio.ps1`) | Legacy; CI uses Gradle |

→ Next: [07 · Version-pinning map](07-version-pinning-map.md)
