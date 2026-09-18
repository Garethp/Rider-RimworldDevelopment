# Backend testing — reference

How the ReSharper SDK test framework is used in this repo: what was learned from JetBrains' own plugins (Unity,
F#, ForTea, the plugin template), one third-party plugin (heapview) and the official docs, and — more usefully —
what it actually took to get a RimWorld XML completion gold test green here. Sources are at the end.

## The approach

Backend completion is tested with the **ReSharper SDK test framework: NUnit + gold files**. A test boots an
in-memory ReSharper shell (once per test assembly), creates an in-memory solution containing the test-data file(s),
runs the feature at `{caret}`, dumps the result to text and diffs it against a committed `.gold` file. First run
writes a `.tmp`; you review it and rename it to `.gold`.

The Kotlin side is not tested. JetBrains' game-engine plugins without backend tests verify completion end-to-end from
Kotlin on TeamCity (full Rider download plus a real .NET SDK per test class); our completion logic is entirely
backend, so that route buys nothing here.

## What exists (all green)

`src/dotnet/ReSharperPlugin.RimworldDev.Tests`:

| Test | Proves |
|---|---|
| `SmokeTests.ShellStarts` | the shell boots |
| `Completion/CSharpCompletionSmokeTests.TestLocalVariable` | `CodeCompletionTestBase` + gold pipeline, no RimWorld involved |
| `Completion/XmlPsiDiagnosticsTests.TestXmlIsParsed` | `.xml` in the in-memory project is parsed as XML PSI; also the `BaseTestWithSingleProject` + `ExecuteWithGold` pattern |
| `Completion/RimworldXmlCompletionTests.TestThingDefProperties` | **our provider**, backed by `Krafs.Rimworld.Ref`, lists all 249 `ThingDef` properties with C# types. Goes red when `GetAllPublicFields` is broken. |

Run: `dotnet test src/dotnet/ReSharperPlugin.RimworldDev.Tests/ReSharperPlugin.RimworldDev.Tests.csproj`
(`--filter FullyQualifiedName~RimworldXmlCompletionTests` for one fixture). ~1 min build with normal `MSB3277`/
`NU1701`/`NU1608` noise, ~15 s of tests.

## How it works, from `dotnet test` to a gold diff

**1. NUnit starts, and one fixture boots a whole ReSharper.** `dotnet test` runs NUnit over our test assembly. NUnit
runs a `[SetUpFixture]` once before any test in its namespace; ours is
`RimworldDevTestsAssembly : ExtensionTestEnvironmentAssembly<…>`, and that base class *is* the ReSharper shell
bootstrapper. It does what Rider's backend process does at startup — build the component container — except
in-process, headless, and once per test run. This is why the SDK copies ~1000 DLLs into the test output: the shell
is assembled from whatever is in that folder.

**2. The component model.** ReSharper doesn't `new` its services; nearly every class is a *component* declared with
an attribute (`[ShellComponent]`, `[SolutionComponent]`, `[PsiComponent]`, `[IntellisensePart]`, …) and created by a
container that satisfies constructor parameters from other components. Our plugin is nothing but components:
`RimworldXMLItemProvider` is one, `RimworldSymbolScope` is one, `RimworlXMLCompletionContextProvider` is one. At
startup the shell *scans* assemblies for these attributes and registers what it finds. Two consequences bit us:
the scanner reads metadata with its own reader (which choked on the net8 `JetBrains.Lifetimes`), and it only scans
assemblies the test assembly references (so the plugin was invisible until a test used a plugin type).

**3. Zones are the on/off switches for components.** Rider, ReSharper, dotCover and JetBrains' tests all share one
component catalogue, and a *zone* is how each host says which parts of it are active. Concretely:

- A **zone definition** is an empty interface/class marked `[ZoneDefinition]`. `IRequire<TZone>` on it means
  "this zone only makes sense if that one is active". Example: JetBrains' `PsiFeatureTestZone` requires the daemon,
  navigation, code-editing, C#, VB, XAML… zones — it is a bundle meaning "everything a PSI feature test needs".
- A **zone marker** is a class named `ZoneMarker` marked `[ZoneMarker]`, and it applies to *every component in its
  namespace and below*: those components are only loaded when all the zones the marker `IRequire<>`s are active.
  The test project's marker requires our env zone, so our test-only components belong to the test host.
- Components with **no marker anywhere above them** are un-zoned and load in every host. That is our plugin's
  situation, and it's why the tests didn't need to declare a plugin zone. (Unity, F# and the template do declare
  one, and then the test env zone must require it, or the plugin's components are filtered out of the test shell.)
- `ITestsEnvZone` is the host zone for "I am a test run". `ExtensionTestEnvironmentAssembly<TZone>` activates the
  zone you give it, and everything it requires, transitively. Ours:
  `RimworldDevTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>` — "this is a test host, and I want
  the full PSI feature set". That one line is what makes C# and XML parsing, completion, the daemon and the
  reference-resolution machinery exist inside the test.

**4. Each test gets an in-memory solution.** `BaseTestWithSingleProject` (which `CodeCompletionTestBase` extends)
builds a temporary solution with one project, puts the named test-data file(s) in it, and adds references. The
project targets .NET 3.5 by default and gets its `mscorlib` etc. from small "platform" NuGet packages the framework
downloads from JetBrains' feed (hence `test/data/nuget.config` and the `NuGetLocks` lock files). Our override of
`GetReferencedAssemblies` appends Krafs' DLLs to that reference list, which is how `ScopeHelper.UpdateScopes` — which
just asks every PSI module "do you have `Verse.ThingDef`?" — finds RimWorld exactly as it does in production.

**5. The feature runs at `{caret}`.** The framework opens the file in an in-memory text control, strips `{caret}`
and puts the caret there, then invokes the real completion pipeline: the context providers build a
`CodeCompletionContext`, every `[IntellisensePart]` items provider whose `IsAvailable` says yes gets `AddLookupItems`
called, and the lookup list is assembled with its normal relevance sorting. Our provider runs unmodified; the only
plugin-side accommodations are the `ScopeHelper` test hooks.

**6. Dump and diff.** `CodeCompletionTestBase` serialises the lookup list (`ModernList` format) to
`<input>.tmp`, compares it with `<input>.gold`, and fails on any difference — or on "no gold file", which is how a
new test's first run hands you the file to review and rename. The framework also fails a test if anything *logged an
error* during it (that's how the xUnit provider and the leaked-cookie problems surfaced), so "logged N errors" in the
output means look at the `Message =` lines, not at your assertion.

## Harness — every line in the csproj is there because of a specific crash

Bootstrap is the template's three types (`RimworldDevTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>`,
`ZoneMarker`, `RimworldDevTestsAssembly : ExtensionTestEnvironmentAssembly<…>`, `[assembly: Apartment(STA)]`).
Every JetBrains example test project is `net472`; ours isn't, and that cost these:

| Setting | Crash it fixed |
|---|---|
| `TargetFramework` **`net10.0-windows`** | Rider 2026.1's backend runs plugins on .NET 10 (the SDK bundles that runtime) and some SDK DLLs are net8-built. A net6 host dies loading them. The plugin's `net6.0` is only a compile target. |
| `UseWindowsForms` + `UseWPF` | `ShellLocks` uses WinForms timers → `FileNotFoundException` in `JetEnvironment.CreateDontRunAsync`. |
| `AssetTargetFallback=net472` | SDK packages only ship props under `build/net472/`. NuGet's default fallback list starts at `net461` and stops at the first framework a package has *any* asset for, so `LibLevelDb` (`lib/net/_._`) never got its props imported and `leveldb.dll` never reached the output. |
| `JetBrains.Microsoft.TestPlatform.TranslationLayer` `ExcludeAssets="all"` | Transitive 2019 net451 VSTest repack whose `Microsoft.TestPlatform.*` DLLs overwrite the ones `testhost` needs → `TypeLoadException` at host start. |
| Post-build copy of **net472** `JetBrains.Lifetimes`/`RdFramework` (`UseNetFrameworkJetBrainsLibs` target) | NuGet gives a .NET host the net8.0 builds; the SDK's component scanner can't read net8.0 Lifetimes metadata ("Error resolving type MaybeNullWhenAttribute… 777.0.0.0") once it scans our plugin. |
| `xunit.runner.utility.net452.dll` copied to output | The SDK's net4x xUnit provider loads it by name; NuGet gives a .NET host the `netcoreapp10` flavour. Every test "logs an error" and fails. |
| Root `Directory.Build.props`: `Lifetimes`/`RdFramework` pinned **2026.1.2** | SDK's exact requirement; floated 2026.1.3 → `MissingMethodException: RdId.Hash` when the protocol component constructs. Rider ships its own copies so production never noticed. |

Packages: `JetBrains.ReSharper.SDK.Tests` (= `$(SdkVersion)`; a 20 KB props file that sets `JetTestProject=True`,
which makes the SDK targets copy ~1000 SDK files into the output), `Microsoft.NET.Test.Sdk`, `NUnit3TestAdapter`,
`GitHubActionsTestLogger` (our Gradle `testDotNet` passes `--logger GitHubActions`). **No explicit NUnit** — the SDK
pins `[3.13.2]` exactly.

**The scanner only loads assemblies the test assembly references.** Until a test used a plugin type, the compiler
emitted no reference to `ReSharperPlugin.RimworldDev.dll` and our components were simply not in the shell. Any
fixture that tests the plugin must touch a plugin type (ours call `ScopeHelper.Reset()`).

Zones: the plugin has no `ZoneMarker`; un-zoned components load everywhere, tests included. XML has no language
zone at all (`JetBrains.ReSharper.Psi.Xml.dll` defines none), so nothing to require. If a plugin zone is ever added,
the test env zone must `IRequire<>` it or every plugin component silently vanishes.

`test/data/nuget.config` is mandatory (framework-reference packages come from `resharper-platform.jetbrains.com`).
`test/data/NuGetLocks/*.lock` **must be committed**: they pin what the *framework* downloads at run time for the
in-memory project (e.g. `JetBrains.Tests.Platform.NETFrameWork 3.5`, requested as an open range), which the csproj
never sees. Without them a new patch upload on JetBrains' feed changes the reference set under the golds. One lock
file per distinct request (file name = hash of the request); delete ones left behind by abandoned experiments.

## Test data

Found by walking up from the test assembly to `test/data`; per fixture `RelativeTestDataPath => @"Completion\Rimworld"`.
`DoNamedTest()` uses the **full method name**: `TestThingDefProperties` → `TestThingDefProperties.xml` (no prefix
stripping; that's `DoNamedTest2`). Gold sits beside the input as `<input>.gold`; `.tmp` appears on mismatch.
`ExecuteWithGold(projectFile, …)` names its gold after the *source file*, so give diagnostics their own input file.
Failure to find the data root looks like "The marker item cannot be found…" then "the Shell is not running".

## Completion tests

```csharp
[TestFileExtension(".xml")]
public class RimworldXmlCompletionTests : CodeCompletionTestBase
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
    protected override string RelativeTestDataPath => @"Completion\Rimworld";
    protected override IEnumerable<string> GetReferencedAssemblies(TargetFrameworkId tfm) => /* base + Krafs DLLs */;
    [SetUp]    public void Reset() { ScopeHelper.Reset(); ScopeHelper.SkipAssemblyDiscovery = true; }
    [TearDown] public void Forget() { ScopeHelper.Reset(); }
    [Test] public void TestThingDefProperties() => DoNamedTest();
}
```

- `ModernList` = gold is the lookup list (`Completion: Basic` / `Count: N` / `Range: "<♦"` / relevance + alphabetic
  sections, `<==` = selected). `Action` = gold is the document after accepting the item named by a
  `// ${COMPLETE_ITEM:name}` header (`ABSENT_ITEM` asserts absence). Input needs `{caret}`.
- Empty result is `Count: 0`; `<NULL RESULT>` means no provider produced a context at all — that is what stock,
  schema-less XML gives in this shell, so there is no "generic XML completion" checkpoint; our provider *is* the
  XML completion here.
- `LookupItemFilter(ILookupItem)` restricts the dump to chosen items (not needed yet: the 249-line ThingDef gold is a
  useful regression net as-is). Also: `Sorting`, `PresentLookupItem`, `[TestSetting(typeof(Key), nameof(Key.Prop), v)]`.
- Gold formats change with SDK versions; expect regeneration on bumps.
- Later: `HighlightingTestBase` (gold = source with `|text|(0)` markers) for the value validators.

## RimWorld types: Krafs.Rimworld.Ref

What works: the test csproj has `<PackageReference Include="Krafs.Rimworld.Ref" ExcludeAssets="all" GeneratePathProperty="true" />`
and bakes `$(PkgKrafs_Rimworld_Ref)\ref\net472` into an `AssemblyMetadataAttribute("RimworldRefDir", …)`; the fixture
overrides `GetReferencedAssemblies` and adds every DLL there except `mscorlib*`/`System*`/`netstandard*`/`Mono.*`.
`ScopeHelper` then finds `Verse.ThingDef` exactly as it does with the real game DLL. The Krafs list is identical to
the real `Assembly-CSharp.dll`'s (the gold was first generated from the latter by accident).

What does not work, so nobody retries it:
- `[TestPackages("Krafs.Rimworld.Ref/1.6.4871")]` restores the package but the in-memory project defaults to
  **.NET 3.5** (see the lock file's `Input (NuGetFramework=net35)`), which can't consume `ref/net472` → nothing referenced.
- `[TestPlatform(".NETFramework", 4, 7, 2)]` needs a `JetBrains.Tests.Platform.NETFrameWork 4.7.2` package; the feed
  stops at 4.6, and the failed restore poisons other fixtures in the run. A net35 project referencing net472-built
  DLLs by path is fine.

Plugin-side hooks added for tests (`internal`, `InternalsVisibleTo` the test assembly):
- `ScopeHelper.Reset()` — the statics otherwise hold scopes from a disposed solution (next test breaks, and the
  framework reports leaked "assembly cookies" at teardown).
- `ScopeHelper.SkipAssemblyDiscovery` — without it `AddRef` finds the developer's real Steam install and adds it to
  the test solution (non-hermetic, and it leaks).
- `UpdateScopes` now looks for `Verse.ThingDef` *before* the "some module has no types → not ready" bail-out; the
  XML-only in-memory project is legitimately empty and used to block RimWorld detection forever.

## When a test fails

Failures come in five shapes; the output tells you which:

| You see | It means | Look at |
|---|---|---|
| "There is no gold file" | new test, expected | the `.tmp` |
| "The test output differs from the gold file" | behaviour changed | `diff` the `.tmp` against the `.gold`; either fix the plugin or accept the new gold |
| `Count: 0` or `<NULL RESULT>` in the `.tmp` | our provider ran but had nothing, or never ran | `ScopeHelper.UpdateScopes` returning `false` (is RimWorld referenced? did `Reset()` run?), then `IsAvailable` |
| "The test has logged N errors" | some component threw during the test; the assertion may even have passed | the `Message =` lines — usually a missing DLL or a component that couldn't construct |
| Every test fails in ~4 s with the same exception | the shell didn't boot | the first `EXCEPTION #1` — it's an environment problem (see the harness table), not a test problem |

## Multi-file (not yet used)

`DoTestSolution([TestName], ["Other.xml"])` for extra files in one project; `DoTestSolution(string[][])` with a project
GUID appended to a file set for a second, referenced project. `SimpleICache`s (e.g. `RimworldSymbolScope`) populate
on solution load; call `psiServices.Files.CommitAllDocuments()` before asserting.

## Gold hygiene

Golds are written with a UTF-8 BOM; commit as-is. Line endings are undocumented — JetBrains forces
`test/data/**/* text eol=lf`; ours is `text=auto`, add the rule before CI runs on Linux. Gitignore `*.tmp` under
test data. Keep input/gold case consistent.

## Platform

JetBrains' last official word (RIDER-23218, 2019): "we don't support plugin unit tests on Linux"; every surveyed
repo runs backend tests on `windows-latest`. Our CI Test job is `ubuntu-latest` and will need to move.

## Diagnostics

- `dotnet msbuild <csproj> -getItem:JetContent -getProperty:JetTestProject` shows what the SDK will copy.
- Reflection over the DLLs in the test output (`ReflectionOnlyLoadFrom`) is the fastest way to find a type's
  namespace or an attribute's constructor — nothing is documented.
- Component/zone filtering: Unity's `TestEnvironment.cs` has a `RESHARPER_LOG_CONF` recipe with TRACE loggers for
  `JetBrains.Application.Environment.JetEnvironment`, `…Extensibility.CatalogComponentSource`,
  `…Environment.RunsProducts`, `…Catalogs.PartCatalogZoneMapping`.
- "The test has logged N errors" fails a test even when its own assertion passed; read the `Message =` lines.

## Sources

Best code references: Unity's
[TestEnvironment.cs](https://github.com/JetBrains/resharper-unity/blob/master/resharper/resharper-unity/test/src/Unity.Tests/TestEnvironment.cs)
(zone comments are the real docs),
[AsmDefReferencesCompletionTests.cs](https://github.com/JetBrains/resharper-unity/blob/master/resharper/resharper-unity/test/src/Unity.Tests/Unity/AsmDef/Feature/Services/CodeCompletion/AsmDefReferencesCompletionTests.cs) +
[gold](https://github.com/JetBrains/resharper-unity/blob/master/resharper/resharper-unity/test/data/Unity/AsmDef/CodeCompletion/AsmDefReferences/TestList01.asmdef.gold),
[TestUnityAttribute.cs](https://github.com/JetBrains/resharper-unity/blob/master/resharper/resharper-unity/test/src/Unity.Tests/Unity/TestUnityAttribute.cs);
F#'s [Common.fs](https://raw.githubusercontent.com/JetBrains/resharper-fsharp/main/ReSharper.FSharp/test/src/FSharp.Tests.Common/src/Common.fs) and
[FSharpCompletionTest.fs](https://raw.githubusercontent.com/JetBrains/resharper-fsharp/main/ReSharper.FSharp/test/src/FSharp.Tests/FSharpCompletionTest.fs);
ForTea's [T4CodeCompletionTest.cs](https://raw.githubusercontent.com/JetBrains/ForTea/master/Backend/RiderPlugin/test/src/T4CodeCompletionTest.cs) +
[Directive.tt.gold](https://raw.githubusercontent.com/JetBrains/ForTea/master/Backend/RiderPlugin/test/data/CodeCompletion/Directive.tt.gold);
heapview's [test csproj](https://raw.githubusercontent.com/controlflow/resharper-heapview/master/src/dotnet/ReSharperPlugin.HeapView.Tests/ReSharperPlugin.HeapView.Tests.csproj) and
[ci.yml](https://raw.githubusercontent.com/controlflow/resharper-heapview/master/.github/workflows/ci.yml);
the [template's Tests project](https://github.com/JetBrains/resharper-rider-plugin/tree/master/content/src/dotnet/ReSharperPlugin.SamplePlugin.Tests).

Official docs worth reading (the rest is skeletal or stale):
[ProjectStructure](https://www.jetbrains.com/help/resharper/sdk/ProjectStructure.html),
[GoldFiles](https://www.jetbrains.com/help/resharper/sdk/GoldFiles.html),
[ExternalAnnotations_Testing](https://www.jetbrains.com/help/resharper/sdk/ExternalAnnotations_Testing.html) (`[TestReferences]`),
[Analysis_Testing](https://www.jetbrains.com/help/resharper/sdk/Analysis_Testing.html).

Negative results: `godot-support` and `azure-tools-for-intellij` have no backend tests (Kotlin end-to-end only, TeamCity);
the docs' `ITestsZone` is stale (`ITestsEnvZone` is current); `[TestPackages]`, `[TestPlatform]`,
`CodeCompletionTestBase` and non-net472 hosts are undocumented anywhere.
