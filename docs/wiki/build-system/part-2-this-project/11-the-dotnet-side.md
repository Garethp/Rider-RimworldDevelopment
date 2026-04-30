# 11 · The .NET side

**[This Project]** — *This is unique to Rider plugins. A standard IntelliJ plugin has no `.csproj` in its build at all.*

This page covers the .NET / C# half of the build: `Directory.Build.props`, the `.csproj` files in general, `global.json`, and how `dotnet build` is wired into the Gradle world.

(The dual-csproj pattern — *why* there are two `.csproj` files for one source tree — gets its own page in §12.)

## `Directory.Build.props` — MSBuild's central config

MSBuild auto-imports a file called `Directory.Build.props` into every `.csproj` in the tree (walking up the directory hierarchy). It's the .NET equivalent of `gradle.properties` for centralized configuration.

This repo's `Directory.Build.props` (54 lines) does five things:

### 1. Pin the SDK version (`:3-4`)

```xml
<PropertyGroup>
  <SdkVersion>2026.1.*</SdkVersion>
  <!-- ... package metadata ... -->
</PropertyGroup>
```

`SdkVersion=2026.1.*` is a **wildcard NuGet version**. `2026.1.*` resolves to the latest 2026.1.x patch on the configured NuGet feed. Used downstream in PackageReferences (`:39-46`).

### 2. Customize MSBuild output paths (`:18-26`)

```xml
<NoPackageAnalysis>true</NoPackageAnalysis>
<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
<ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch>None</ResolveAssemblyWarnOrErrorOnTargetArchitectureMismatch>

<BaseIntermediateOutputPath>obj\$(MSBuildProjectName)\</BaseIntermediateOutputPath>
<DefaultItemExcludes>$(DefaultItemExcludes);obj\**</DefaultItemExcludes>
<OutputPath>bin\$(MSBuildProjectName)\$(Configuration)\</OutputPath>
```

Notable:
- `<OutputPath>bin\$(MSBuildProjectName)\$(Configuration)\</OutputPath>` — output goes to `bin/<csproj-name>/<Configuration>/`. So building `ReSharperPlugin.RimworldDev.Rider.csproj` with Release config produces `bin/ReSharperPlugin.RimworldDev.Rider/Release/`. This is what Gradle's `prepareSandbox` reads from (`build.gradle.kts:159`).
- `<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>` — by default MSBuild adds `/net6.0/` to the output path; this turns it off, keeping paths short.
- The "WarnOrError on architecture mismatch" suppression silences false positives common in mixed-architecture NuGet packages.

### 3. Configuration-conditional defines (`:28-30`)

```xml
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <DefineConstants>TRACE;DEBUG;JET_MODE_ASSERT</DefineConstants>
</PropertyGroup>
```

In Debug, additional `#define`s are set. `JET_MODE_ASSERT` enables JetBrains assertion macros. Release builds don't get these.

### 4. Compute `WaveVersion` (`:32-36`)

```xml
<PropertyGroup>
  <WaveVersionBase>$(SdkVersion.Substring(2,2))$(SdkVersion.Substring(5,1))</WaveVersionBase>
  <WaveVersion>$(WaveVersionBase).0.0$(SdkVersion.Substring(8))</WaveVersion>
  <UpperWaveVersion>$(WaveVersionBase).9999.0</UpperWaveVersion>
</PropertyGroup>
```

For `SdkVersion=2026.1.*`:
- `Substring(2,2)` = `"26"`, `Substring(5,1)` = `"1"` → `WaveVersionBase = "261"`
- `Substring(8)` of `"2026.1.*"` = `"*"` → `WaveVersion = "261.0.0*"`

Wave is ReSharper's internal version stream. Wave 261 = ReSharper 2026.1, Wave 252 = 2025.2. The `Wave` NuGet package referenced by the Wave/ReSharper csproj uses this. JetBrains' Marketplace enforces Wave-compatible NuGet manifests at upload.

### 5. Pin the JetBrains NuGet packages (`:38-47`)

```xml
<ItemGroup>
  <PackageReference Include="JetBrains.Annotations" Version="2025.2.4" />
  <PackageReference Include="JetBrains.Lifetimes" Version="$(SdkVersion)" />
  <PackageReference Include="JetBrains.RdFramework" Version="$(SdkVersion)" />
  <PackageReference Include="System.Diagnostics.TraceSource" Version="4.3.0" />
  <PackageReference Include="JetBrains.Rider.SDK" Version="$(SdkVersion)">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

Every `.csproj` inheriting these props gets:
- `JetBrains.Annotations` — `[NotNull]`, `[CanBeNull]`, etc. attributes
- `JetBrains.Lifetimes` — RD lifetime management
- `JetBrains.RdFramework` — RD runtime
- `JetBrains.Rider.SDK` — Rider extension APIs

`Lifetimes` and `RdFramework` are **explicitly pinned to `$(SdkVersion)`** even though they'd come in transitively. Why: the RD `serializationHash` in the generated C# (RemodderProtocolModel.Generated.cs:73 in this codebase) is computed against a specific RdFramework version. If transitive resolution picks a different RdFramework patch, frontend and backend hashes mismatch and the protocol bind silently fails. Explicit pinning is a binary-compat backstop.

`PrivateAssets="all"` on the Rider SDK means "consumers of this project don't transitively get the Rider SDK." `IncludeAssets="runtime; build; ..."` lists what *this* project pulls in. Together they say "I get everything; consumers get nothing" — appropriate for a leaf plugin assembly.

## `global.json` — pin the .NET SDK

```json
{
  "sdk": {
    "version": "7.0.202",
    "rollForward": "latestMajor",
    "allowPrerelease": true
  }
}
```

`dotnet` CLI reads this and uses .NET SDK 7.0.202+ (with `rollForward: latestMajor` it'll roll up to whatever's installed in the 7.x or higher line). On a CI runner with .NET 8 or 9 installed, this is fine.

## How Gradle and dotnet interact

Three Gradle `Exec` tasks bridge the worlds:

| Task | Command | What it produces |
|---|---|---|
| `compileDotNet` (`build.gradle.kts:86-90`) | `dotnet build --configuration Release` | Built DLLs in `bin/<csproj>/Release/` |
| `buildResharperPlugin` (`:92-105`) | `dotnet msbuild $sln /t:Restore;Rebuild;Pack /p:PackageOutputPath=output /p:PackageVersion=$ver` | `output/ReSharperPlugin.RimworldDev.<ver>.nupkg` (Wave flavour only) |
| `testDotNet` (`:226-230`) | `dotnet test $sln --logger GitHubActions` | Test results in stdout |

All three run from the repo root (`workingDir(rootDir)`). All three use `executable("dotnet")`, relying on `dotnet` being on PATH.

## What ends up in the plugin ZIP

After `compileDotNet`, the on-disk layout in `bin/` is roughly:

```
bin/
├── ReSharperPlugin.RimworldDev/Release/             ← Wave/ReSharper flavour
│   └── ReSharperPlugin.RimworldDev.dll
└── ReSharperPlugin.RimworldDev.Rider/Release/       ← Rider flavour
    ├── ReSharperPlugin.RimworldDev.dll              (NOTE same name, different content)
    ├── ReSharperPlugin.RimworldDev.pdb
    ├── 0Harmony.dll
    ├── AsmResolver.dll
    ├── AsmResolver.DotNet.dll
    ├── AsmResolver.PE.dll
    ├── AsmResolver.PE.File.dll
    ├── ICSharpCode.Decompiler.dll
    └── (other transitive runtime DLLs)
```

`prepareSandbox` reads from the `.Rider/Release/` folder and copies a hand-picked subset into the plugin's `dotnet/` folder. See §14.

## Why .NET 6 and not .NET 8 or 9?

The `<TargetFramework>net6.0</TargetFramework>` in both `.csproj` files matches what the Rider 2026.1 backend host runs on. Plugins must target a TFM compatible with the host. JetBrains documents this in IPGP / Rider plugin docs. Bumping requires JetBrains to bump the host first.

## .NET package: how it ships separately

The `Pack` MSBuild target on the Wave csproj produces `output/ReSharperPlugin.RimworldDev.<ver>.nupkg`. That `.nupkg` ships to the Marketplace under a separate plugin id (`ReSharperPlugin.RimworldDev`) for ReSharper-for-VS users. The Rider users get the `rimworlddev-<ver>.zip` instead. Two listings, two artifacts. Discussed in §16.

→ Next: [12 · Dual-csproj pattern](12-dual-csproj-pattern.md)
