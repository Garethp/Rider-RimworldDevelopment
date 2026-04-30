# 12 · Dual-csproj pattern

**[This Project]** — *This is a JetBrains-plugin-specific convention. Standard .NET projects almost never do this.*

Two `.csproj` files share the same source tree at `src/dotnet/ReSharperPlugin.RimworldDev/`. They produce two different DLLs from the same `.cs` files, gated by preprocessor symbols and `<Compile Remove>` rules. This page explains why, and what each csproj uses.

## The two flavours

| File | Flavour | Target consumer | Packs? |
|---|---|---|---|
| `ReSharperPlugin.RimworldDev.csproj` | "Wave" / ReSharper-for-VS | Visual Studio + ReSharper | Yes (produces a `.nupkg`) |
| `ReSharperPlugin.RimworldDev.Rider.csproj` | Rider | JetBrains Rider | No (consumed by Gradle's `prepareSandbox`) |

Both produce a DLL named **`ReSharperPlugin.RimworldDev.dll`** (yes, the same name) but they go to different output folders thanks to the `<OutputPath>bin\$(MSBuildProjectName)\$(Configuration)\</OutputPath>` rule in `Directory.Build.props:25`.

## What's actually different

### Wave / ReSharper (`ReSharperPlugin.RimworldDev.csproj`)

```xml
<TargetFramework>net6.0</TargetFramework>
<IsPackable>True</IsPackable>
<DefineConstants>$(DefineConstants);RESHARPER</DefineConstants>
<IncludeBuildOutput>false</IncludeBuildOutput>

<PackageReference Include="JetBrains.ReSharper.SDK" Version="$(SdkVersion)" PrivateAssets="all" />
<PackageReference Include="Wave" Version="$(WaveVersion)" />

<!-- Custom packaging: hand-place DLL+PDB into the nupkg's dotFiles/ folder -->
<Content Include="bin\$(AssemblyName)\$(Configuration)\$(AssemblyName).dll" PackagePath="dotFiles" Pack="true" />
<Content Include="bin\$(AssemblyName)\$(Configuration)\$(AssemblyName).pdb" PackagePath="dotFiles" Pack="true" />

<!-- Exclude features that are Rider-only or use Rider-only APIs -->
<Compile Remove="TemplateParameters\**" />
<Compile Remove="RimworldXmlProject\**" />
<Compile Remove="projectTemplates\**" />
<Compile Remove="Remodder\**" />
```

Notable choices:
- **`DefineConstants RESHARPER`** — the source can do `#if RESHARPER ... #endif` for Wave-specific code paths
- **`IsPackable=True`** + custom `<Content>` items — produces a `.nupkg` with the DLL+PDB at `dotFiles/` (the path layout the Marketplace expects for ReSharper plugins)
- **`<PackageReference Include="JetBrains.ReSharper.SDK">`** — pulls in *only* the ReSharper SDK (no Rider-specific APIs). Note that this overrides the project-wide `JetBrains.Rider.SDK` from `Directory.Build.props:43` for this csproj.
- **`<PackageReference Include="Wave">`** — declares Wave-version compatibility. The Marketplace gates ReSharper plugin uploads on Wave compatibility metadata.
- **`<Compile Remove="Remodder\**" />`** etc. — physically excludes those source folders from compilation. The Remodder, RimworldXmlProject, and ProjectTemplates features use Rider-specific APIs (RD protocol, project model) that aren't available in plain ReSharper. So they're excluded from the Wave build entirely.

### Rider (`ReSharperPlugin.RimworldDev.Rider.csproj`)

```xml
<TargetFramework>net6.0</TargetFramework>
<AssemblyName>ReSharperPlugin.RimworldDev</AssemblyName>
<RootNamespace>$(AssemblyName)</RootNamespace>
<IsPackable>false</IsPackable>
<DefineConstants>$(DefineConstants);RIDER</DefineConstants>

<!-- This is needed to force our dependant DLLs to be present in the build folder, which we then copy over in gradle -->
<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>

<!-- Remodder-only NuGets -->
<PackageReference Include="AsmResolver" Version="5.5.1" />
<PackageReference Include="AsmResolver.DotNet" Version="5.5.1" />
<PackageReference Include="ICSharpCode.Decompiler" Version="8.2.0.7535" />
<PackageReference Include="Krafs.Publicizer" Version="2.0.1" />
<PackageReference Include="Lib.Harmony" Version="2.3.3" />

<Publicize Include="0Harmony;" />
```

Notable choices:
- **`<AssemblyName>ReSharperPlugin.RimworldDev</AssemblyName>`** — forces the output DLL filename to match the Wave csproj's, so consumer code can reference `ReSharperPlugin.RimworldDev.dll` regardless of flavour. The csproj **filename** is `.Rider.csproj`; the **assembly name** is plain.
- **`DefineConstants RIDER`** — for code paths that should compile in Rider but not in plain ReSharper
- **`IsPackable=false`** — this DLL is not published to the Marketplace as a `.nupkg`. It's consumed by Gradle's `prepareSandbox` and packaged into the Rider plugin ZIP.
- **`<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>`** — without this, NuGet transitive DLLs (AsmResolver, ICSharpCode.Decompiler, etc.) wouldn't appear in `bin/`. The flag forces them into `bin/`, where Gradle's `prepareSandbox` (`build.gradle.kts:160-171`) finds and copies them. Comment in the csproj literally says: *"This is needed to force our dependant DLLs to be present in the build folder, which we then copy over in gradle."*
- **Remodder NuGets** — included only here. The Wave flavour excludes Remodder code via `<Compile Remove>`, so it doesn't need these.
- **`<Publicize Include="0Harmony;" />`** — Krafs.Publicizer rewrites `0Harmony.dll`'s metadata to make private members public, so this code can reflect into Harmony internals. Harmless; affects the in-build copy only.

## Decision matrix: where does new code go?

| New feature uses... | Where to add it | Compile-Remove notes |
|---|---|---|
| ReSharper-only APIs (analyzer, completion item provider) | Both flavours, no exclusion | Code shared as-is |
| Rider-only APIs (project model, RD protocol, IRiderTooling) | Rider flavour only | Add file under e.g. `Remodder/` and ensure the Wave csproj excludes via `<Compile Remove="Remodder\**" />` |
| Decompiler / AsmResolver | Rider only | Same |
| Pure C# logic, no IDE APIs | Both | Code shared as-is |
| `#if RESHARPER` vs `#if RIDER` | Both, with conditional code | Use sparingly; prefer file-level exclusion |

When you add a *new* folder of Rider-only code, remember to add the matching `<Compile Remove="YourFolder\**" />` to the Wave csproj. Otherwise the Wave build will try to compile it and fail.

## Why not just one csproj with `#if`?

You could. JetBrains' template historically used both approaches. The dual-csproj pattern wins when:

- Most of the divergent code is large (whole folders), making `#if` blocks unwieldy
- The package metadata differs significantly (different `PackageReference`s, different `IsPackable` story)
- You want clean separation between "what ships to ReSharper" and "what ships to Rider"

This plugin makes that trade-off the right way: the Remodder feature uses heavy Rider-specific APIs and pulls in 5 NuGets that the Wave build doesn't need. Excluding it as a folder is cleaner than `#if`-fencing every method.

## How the build picks the right one

- `dotnet build` (the `compileDotNet` task) builds **everything** in the solution — both csprojs. Both DLLs end up in `bin/`.
- `dotnet msbuild .../$sln /t:Pack` (the `buildResharperPlugin` task) packs only csprojs with `IsPackable=true` — i.e., only the Wave one. Result: one `.nupkg`.
- `prepareSandbox` reads from `bin/ReSharperPlugin.RimworldDev.Rider/Release/` (`build.gradle.kts:159`) — so the Rider flavour is what gets bundled into the Rider plugin ZIP.
- `Deploy.yml` ships the ZIP (Rider) and the `.nupkg` (ReSharper) to the same Marketplace under different listings.

→ Next: [13 · The riderModel bridge](13-the-riderModel-bridge.md)
