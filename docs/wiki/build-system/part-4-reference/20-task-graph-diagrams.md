# 20 · Task graph diagrams

**[Reference]** — *Visual reference for how tasks chain together. Cross-reference §09 (annotated build), §14 (prepareSandbox), §16 (CI).*

## Task DAG (the bare essentials)

```mermaid
graph LR
    initSdk[initializeIntelliJPlatformPlugin<br/>downloads + extracts Rider SDK]
    rdgen[":protocol:rdgen<br/>Model.kt → .Generated.kt + .Generated.cs"]
    compileDotNet[compileDotNet<br/>dotnet build]
    compileKotlin[compileKotlin]
    prepareSandbox[prepareSandbox<br/>copy DLLs + JARs into fake plugins dir]
    runIde[runIde<br/>launch Rider against sandbox]
    buildPlugin[buildPlugin<br/>zip the sandbox]
    patchPluginXml[patchPluginXml<br/>inject version + changelog]
    publishPlugin[publishPlugin<br/>upload to Marketplace]
    testDotNet[testDotNet<br/>dotnet test]
    buildResharper[buildResharperPlugin<br/>dotnet msbuild Pack]

    initSdk --> compileKotlin
    initSdk --> rdgen
    rdgen -.manual.-> compileKotlin
    rdgen -.manual.-> compileDotNet
    compileKotlin --> patchPluginXml
    patchPluginXml --> prepareSandbox
    compileDotNet --> prepareSandbox
    prepareSandbox --> runIde
    prepareSandbox --> buildPlugin
    testDotNet --> publishPlugin
    buildPlugin --> publishPlugin
    buildResharper -.standalone.-> nupkg[output/*.nupkg]
```

Notes:
- `rdgen` is shown with dashed `-.manual.-` edges because it's NOT in the build path of `:buildPlugin` / `compileKotlin`. The arrow indicates that *if you run rdgen, the regenerated files become inputs to compileKotlin/compileDotNet.* But the build doesn't trigger rdgen automatically.
- `buildResharperPlugin` is standalone — only invoked when explicitly requested or by `Deploy.yml`.
- `compileDotNet` re-runs every build (no `@OutputFiles` declared). The arrow into `prepareSandbox` is a `dependsOn`, not an incremental input/output relationship.

To verify against your local build:

```bash
./gradlew runIde --dry-run
./gradlew buildPlugin --dry-run
./gradlew publishPlugin --dry-run
```

If the diagram diverges from `--dry-run` output, update the diagram (it's authored at a point in time; reality is the source of truth).

## File flow (where bytes physically move)

```mermaid
graph TB
    src_cs["src/dotnet/...*.cs<br/>(C# sources)"]
    src_kt["src/rider/main/kotlin/...*.kt<br/>(Kotlin sources)"]
    model["protocol/.../Model.kt<br/>(rdgen DSL)"]

    gen_cs[src/dotnet/.../*.Generated.cs]
    gen_kt[src/rider/main/kotlin/remodder/*.Generated.kt]

    dll["bin/.../ReSharperPlugin.RimworldDev.dll<br/>+ 0Harmony.dll, AsmResolver*.dll, etc."]
    jar[build/libs/rimworlddev-X.Y.Z.jar]

    sandbox[build/idea-sandbox/plugins/rimworlddev/<br/>  +-- lib/ jars<br/>  +-- dotnet/ dlls<br/>  +-- ProjectTemplates/]

    zip[output/rimworlddev-X.Y.Z.zip]
    nupkg[output/ReSharperPlugin.RimworldDev.X.Y.Z.nupkg]
    rider[Rider IDE running the sandbox]

    model -->|rdgen| gen_cs
    model -->|rdgen| gen_kt
    src_cs -->|dotnet build| dll
    gen_cs --> src_cs
    gen_kt --> src_kt
    src_kt -->|compileKotlin + jar| jar
    dll -->|prepareSandbox copy| sandbox
    jar -->|prepareSandbox copy| sandbox
    sandbox -->|buildPlugin zips| zip
    sandbox -->|runIde loads| rider
    src_cs -->|dotnet msbuild Pack| nupkg
```

## Version-pinning map (which file controls which artifact)

```mermaid
graph LR
    gp[gradle.properties]
    tom[gradle/libs.versions.toml]
    dbp[Directory.Build.props]
    bgk[build.gradle.kts inline]
    gjs[global.json]

    gp -->|PluginVersion| pluginzip[Plugin zip filename + plugin.xml version]
    gp -->|ProductVersion| ridersdk[Rider SDK download via IPGP]
    gp -->|rdVersion| rdgenplugin[com.jetbrains.rdgen Gradle plugin]
    gp -->|rdKotlinVersion| pmkotlin[Kotlin in pluginManagement]
    gp -.DUP.-> ipgp
    bgk -->|inline declaration| ipgp[IntelliJ Platform Gradle Plugin]
    gp -.DUP DRIFTED.-> wrap
    bgk -->|inline declaration| wrap[gradle-jvm-wrapper plugin]
    tom -->|kotlin| kotlinplugin[Kotlin Gradle plugin and stdlib]
    tom -->|rdGen| rdgenlib[rd-gen library in :protocol]
    dbp -->|SdkVersion| nugetpkgs[JetBrains.Lifetimes / RdFramework / Rider.SDK NuGets]
    dbp -->|WaveVersion computed| wavepkg[Wave NuGet for ReSharper-for-VS]
    gjs -->|sdk.version| dotnet[.NET SDK that runs dotnet build]
```

The dashed `DUP` and `DUP DRIFTED` edges mark places where the same property is pinned in two files. §07 has the full table.

## Sandbox layout (what `prepareSandbox` produces)

```
build/idea-sandbox/
├── config/                                   ← IDE settings
├── system/
│   └── log/
│       ├── idea.log                          ← JVM frontend logs
│       └── Rider.Backend/log/                ← .NET backend logs
└── plugins/
    └── rimworlddev/                          ← OUR PLUGIN
        ├── lib/
        │   ├── rimworlddev-X.Y.Z.jar         ← compiled Kotlin
        │   └── (transitive JARs)
        ├── dotnet/                            ← LOAD-BEARING; Rider sweeps this for *.dll
        │   ├── ReSharperPlugin.RimworldDev.dll
        │   ├── ReSharperPlugin.RimworldDev.pdb
        │   ├── 0Harmony.dll
        │   ├── AsmResolver.dll
        │   ├── AsmResolver.DotNet.dll
        │   ├── AsmResolver.PE.dll
        │   ├── AsmResolver.PE.File.dll
        │   └── ICSharpCode.Decompiler.dll
        ├── ProjectTemplates/
        │   └── RimworldProjectTemplate/
        │       └── ...
        └── META-INF/
            └── plugin.xml                     ← patched at build by patchPluginXml
```

## Distribution flow (CI publish)

```mermaid
graph TB
    tag[Tag push e.g. 2025.1.11]
    deploy[Deploy.yml runs]
    publishGr[gradle :publishPlugin]
    buildRsh[gradle :buildResharperPlugin]
    nugetPush[dotnet nuget push]
    ghRel[gh release upload]

    rZip[output/rimworlddev-2025.1.11.zip]
    rNupkg[output/ReSharperPlugin.RimworldDev.2025.1.11.nupkg]

    mpRider[JetBrains Marketplace<br/>com.jetbrains.rider.plugins.rimworlddev]
    mpReSharper[JetBrains Marketplace<br/>ReSharperPlugin.RimworldDev]
    ghAssets[GitHub Release assets]

    tag --> deploy
    deploy --> publishGr
    deploy --> buildRsh
    publishGr --> rZip
    buildRsh --> rNupkg
    rZip --> mpRider
    rNupkg --> nugetPush
    nugetPush --> mpReSharper
    rZip --> ghRel
    rNupkg --> ghRel
    ghRel --> ghAssets
```

→ Next: [21 · Contributed tasks table](21-contributed-tasks-table.md)
