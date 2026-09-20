# Backend testing — broadening plan

The first round (see `testing-research.md`) proved one path: `CodeCompletionTestBase` + gold files, Krafs referenced
into the in-memory project, `ScopeHelper` finding `Verse.ThingDef`, and `RimworldXMLItemProvider` listing one type's
fields at the top level of a def.

> **Status (2026-09-18): experiment phase closed after Phase F.** See "Conclusions" at the end. Phases G and H were
> not run; they remain here as the map for whoever picks this up.

This plan is about finding **where the harness stops working**, not about coverage. Each step changes one thing
relative to something already green, so a failure points at that one change. Stop at the first red step, fix or
record the boundary, then carry on.

Status column: `—` not started, `✅` green, `❌` boundary found (see notes), `⚠️` green with caveats.

## Phase A — same fixture, same single file, harder paths through the provider

Only the input XML changes; the C# fixture is untouched.

| # | Step | New variable | Status |
|---|---|---|---|
| 1 | Nested field: `<ThingDef><building><{caret}` | `GetContextFromHierachy` following a field into its type | ✅ |
| 2 | `<li>` of a list: `<ThingDef><comps><li><{caret}` | `"li"` resolved via previous field's `List<T>` type argument | ✅ |
| 3 | `<li Class="CompProperties_Power"><{caret}` | the `li<X>` branch, class resolved against RimWorld's scope | ✅ |
| 4 | Text value, enum: `<category>{caret}</category>` | the `TEXT` branch of `IsAvailable`/`AddLookupItems` | ✅ |
| 5 | Accept an item (`CodeCompletionTestType.Action` + `${COMPLETE_ITEM:…}`) | gold is the document after insertion | ❌ |

## Phase B — the def index (`RimworldSymbolScope`)

Still completion, but now dependent on a `SimpleICache` populating inside the test shell.

| # | Step | New variable | Status |
|---|---|---|---|
| 6 | Def reference, same file: caret in a field typed as a `Def` subclass, target def in the same file | `RimworldSymbolScope` indexing in a test solution | ✅ |
| 7 | Same, target def in a second file | `DoTestSolution(name, ["Other.xml"])` — first multi-file test | ✅ |
| 8 | `ParentName=""` completion with `Abstract="true"` and concrete defs | attribute branch + `DefTags` abstract flag | ✅ |
| 9 | Mod-defined def class in a `.cs` file (`MyMod.CustomThingDef : ThingDef`, `li Class="MyMod.Foo"`) | mixed C#/XML project, `ExtraDefTagNames`, all-solution-scopes fallback | ⚠️ |

## Phase C — the same pipeline from the C# side

Phase B's XML + index setup, caret in a `.cs` file, different providers.

| # | Step | New variable | Status |
|---|---|---|---|
| 10 | `[DefOf]` field completion against defs in a companion `.xml` | `CSharpDefsOfItemProvider` | ⚠️ |
| 11 | `DefDatabase<ThingDef>.GetNamed("{caret}")` | `RimworldDefCSharpItemProvider` | ✅ |

## Phase D — reference resolution (Ctrl+Click): a new kind of test

| # | Step | New variable | Status |
|---|---|---|---|
| 12 | Hand-rolled dump on `BaseTestWithSingleProject` + `ExecuteWithGold` (proven by `XmlPsiDiagnosticsTests`): every tag → `GetReferences()` → `Resolve()` → declared element | `RimworldReferenceProvider` + `RimworldXmlReference` (XML → C# field) | ✅ |
| 13 | Same dump over Phase B input | `RimworldXmlDefReference` (XML → XML def) | ✅ |
| 14 | Same dump over Phase C input | `RimworldCSharpReferenceProvider` (C# string → XML def) | ✅ |
| 15 | (Optional) port 12–14 onto the SDK's own reference test base, if one exists — find it by reflection | the SDK base class | ✅ |

## Phase E — daemon / highlighting

| # | Step | New variable | Status |
|---|---|---|---|
| 16 | One invalid `bool` via `HighlightingTestBase` (gold = source with `\|text\|(0)` markers) | `CustomXmlAnalysisStage` in the test shell; daemon registration, severity filter | ✅ |
| 17 | No RimWorld reference → no highlights, no logged errors | the bail-out path; first test without Krafs | ✅ |
| 17b | Every validating branch across a pair of same-shaped inputs, valid and invalid: int, float, IntRange, FloatRange, Vector2, Vector3, enum, struct, the types the stage skips, unresolvable fields and whitespace-padded values | the rest of the switch in `CustomXmlAnalysisStageProcess`; found boundaries 8 and 9 | ✅ |
| 17c | The same suite again in a mod's real layout — XML in a referenceless project, RimWorld referenced by a C# project beside it | `[ProjectLayouts]` + `ProjectLayoutSupport`; project descriptors and per-project references | ✅ |

## Phase F — Find Usages (known rough edge)

| # | Step | New variable | Status |
|---|---|---|---|
| 18 | Find Usages on a `<defName>`, usages in another XML file and a C# file | `XMLTagDeclaredElement`, `RimworldSearcherFactory`/`CustomSearcher`. Read `Find Usages.md` first; may end up documenting current behaviour rather than correct behaviour | ⚠️ |

## Phase G — environment-driven behaviour

| # | Step | New variable | Status |
|---|---|---|---|
| 19 | Drop Krafs from references, `SkipAssemblyDiscovery = false`, copy Krafs' `Assembly-CSharp.dll` into a temp `RimWorldWin64_Data/Managed/`, point `RimworldPath` at it via `[TestSetting]`, rerun step 1's input | `ScopeHelper.AddRef` / `IAssemblyFactory.AddRef` / settings accessor — the real XML-only-mod mechanism. Expect teardown cookie/leak issues | — |
| 20 | Alt+Insert property generator via the SDK's generate test base | the generate workflow; `PropertyOrdering` makes gold order meaningful | — |

## Phase H — beyond the current test project (boundary-finding only)

| # | Step | New variable | Status |
|---|---|---|---|
| 21 | Test one trivial Rider-only thing (e.g. `RimworldProjectMark` parsing an `About.xml`) | the test project references the **RESHARPER** csproj, which excludes `RimworldXmlProject/`, `Remodder/`, `TemplateParameters/`; can a test project reference the Rider csproj and still boot? | — |
| 22 | Remodder `Decompiler` against a tiny Harmony-patched assembly | mostly non-PSI; cheap once 21 works | — |
| 23 | (Separate track) plain JUnit for `QuickStartUtils` setup/teardown against a temp Ludeon dir | Kotlin side, no IDE; highest-stakes code (it can destroy the user's mod list) | — |

Out of scope until we decide what to keep: moving CI to Windows, the `.gitattributes` LF rule for golds.

## Reading the results

- A–C green → the completion harness generalises as-is.
- D/E → whether non-completion SDK test bases work on our `net10.0-windows` host.
- G → whether anything touching disk and settings can be made hermetic.
- H → whether the Rider-only build is testable at all.

After each phase, fold what broke and how it was fixed into `testing-research.md`.

## Findings log

### Phase A–C (2026-09-18) — the completion harness generalises; the bugs found are the plugin's

Test inputs/golds: `test/data/Completion/Rimworld/` (A, B), `…/Rimworld/Action/` (step 5), `…/RimworldCSharp/` (C).
Fixtures share `RimworldCompletionTestBase` (Krafs references + `ScopeHelper` reset). Suite: 15 green, 4 `[Ignore]`d
with a reason pointing here. Nothing needed changing in the harness itself.

**Worked unchanged (✅):** nested fields (1), `<li>` via `List<T>` (2), `li Class=` (3), enum text values (4), def
references from the same file (6) and a second file (7, `DoNamedTest("Other.xml")`), `ParentName` abstract-only
filtering (8), a mod `.cs` file alongside XML in the same in-memory project (9a/9b — `MyMod.CustomThingDef` as a def
root, `li Class="MyMod.CompProperties_Custom"`), `DefDatabase<ThingDef>.GetNamed("…")` (11), `[DefOf]` with
`Mod{caret};` (10). `RimworldSymbolScope` populates during the test solution's load with no extra plumbing; no
`CommitAllDocuments` needed for list tests.

**Step 5 ❌ — plugin bug, not harness.** `CodeCompletionTestType.Action` works (the `${COMPLETE_ITEM:x}` directive can
sit in an XML comment; the inserted text in both golds is right), but accepting an item commits the document, and
`RimworldSymbolScope.Merge` → `AddToLocalCache` calls `sourceFile.GetPrimaryPsiFile()` *during* the commit's merge
phase. The platform logs "Trying to get PSI file for an uncommitted document" (`PsiFiles.AssertNotDirty`) twice, and
the framework fails any test that logs errors. First load doesn't trip it because nothing is dirty yet. This will
affect *any* future test that edits an XML document (Action completion, quick-fixes, generators). Root cause is the
index design: it resolves persisted offsets back to live `ITreeNode`s at merge time. Fix candidates: resolve lazily at
query time, or defer resolution until after commit.

**Fixed (2026-09-18):** the index now stores `(sourceFile, offset, isAbstract)` and `GetTagByDef` finds the node on
query (`isAbstract` is computed in `Build` and persisted). Both Action tests pass unchanged against their golds.
`SymbolScope/RimworldSymbolScopeTests` edits a def file and checks the node handed back is the live one at the def's new
offset, including a def moving onto another def's old offset (checked by removing the cached-node check: it fails).

**Step 9c ⚠️ — plugin bug (load order).** A `MyMod.CustomThingDef` def is not offered where a `ThingDef` is expected.
Traced: when the index merges on load, `ScopeHelper.RimworldScope` is still `null` (symbol caches not ready), so
`AddDefTagToList` skips building `ExtraDefTagNames`, and nothing rebuilds it later. Likely also real on a cold open in
Rider (custom def subclasses unresolved until their file is edited) — not verified in Rider. Test has a hand-written
expected gold and is ignored.

**Fixed (2026-09-18)** with step 5: `ExtraDefTagNames` is rebuilt on the first query after a custom-typed def changes,
and stays stale until the scopes are ready and every custom def type resolves. The hand-written gold now passes.

**Step 10 ⚠️ — narrow `IsAvailable`.** `CSharpDefsOfItemProvider` requires the caret's parent to be an
`IFieldDeclaration`. With `public static ThingDef Mod{caret}` followed by `}` (i.e. typing a new field at the end of
the class, the natural moment to complete), C# error recovery parses a **`MethodDeclaration`**; with no name typed the
caret node is whitespace. Only `Mod{caret};` works. Same parser as Rider, so presumably the same in production. Test
for the unfinished shape has a hand-written expected gold and is ignored.

**Found by reading, not by a test:** `ScopeHelper.GetScopeForClass` searches `knownCustomScopes` twice; the second
lookup was meant to search `allScopes`, so `knownCustomScopes` is never populated and every dotted class name falls
back to `rimworldScope`. Masked whenever the mod's types live in the same module as the RimWorld reference (single
project, and every test here) because that scope includes references. A test for it needs a *second* project holding
the mod types — `DoTestSolution(string[][])`.

**Technique that paid off:** a temporary `File.AppendAllText(Path.GetTempPath()/"rw-trace.txt", …)` in the plugin
method under test, then revert. Faster than reading framework logs to find which gate in `IsAvailable` rejected.

### Phase D (2026-09-18) — reference resolution and real navigation both testable

Tests: `References/RimworldReferenceTests.cs` (dump, steps 12–14) and `References/RimworldNavigationTests.cs` (step 15);
data under `test/data/References/`. Suite: 20 green, 4 ignored (unchanged from A–C).

**Steps 12–14 ✅, first run.** The dump (`BaseTestWithSingleProject`, walk `psiFile.Descendants()`, call
`node.GetReferences<IReference>()`, `Resolve()`, write one line per reference) picks up our
`IReferenceProviderFactory`s with no registration work. The dump is filtered to reference types from the plugin
assembly, because a C# file otherwise lists every ordinary type/namespace reference. What resolves: def type → class,
inherited/nested/`li`/`li Class` fields → `IField`, enum text → enum member, `Name`/`ParentName`/`defName` → the def's
`XMLTagDeclaredElement`, a def reference to a def in the same file / another file / another def type, and C# `[DefOf]`
fields and `DefDatabase<T>.GetNamed*("…")` strings → the XML def. Missing defs and wrong-type defs are not linked (by
design; there's no "unresolved" reference).

**Minor bug (recorded in the step 13 gold):** closing tags get Ctrl+Click only by coincidence. `GetHierarchy` treats a
closing `</minifiedDef>` identifier as *inside* its own tag, so it looks up `minifiedDef` on the field's *type*
(`ThingDef`), which happens to have that field. `</soundDrop>` etc. get nothing.

**Gaps, not bugs:** no reference on the `li Class="…"` value, and none on `Type`-typed text (`<compClass>CompGlower`).

**Step 15 ✅, better than planned.** The SDK ships no generic reference/resolve test base (only
`WebReferenceTestBase`; JetBrains' own resolve tests are in unpublished assemblies). But
`JetBrains.ReSharper.IntentionsTests.Navigation.AllNavigationProvidersTestBase` works: it runs *every*
context-navigation provider at an `{on}` marker (not `{caret}`) and dumps what each would do. It needs
`ExtraPath => ""` overridden. Its golds show Go to Declaration/Implementation/Type Declaration opening **decompiled Krafs
source** (`ThingDef.cs`, caret on the field), Go to Declaration on a def reference landing on the `<defName>`, and
**Find Usages / Show Usages / Highlight Usages through `CustomSearcher`** finding both occurrences of a def. So Phase F
is partly proven already: the searcher runs in the test shell. Note Find Usages on a C# field from XML reports "not
found" (usages of C# members in XML aren't searchable — expected, per `Find Usages.md`).

Gold stability: the navigation golds embed decompiled Krafs source and some HTML-ish menu markup — pinned by the
Krafs version and the SDK version respectively; expect churn on bumps.

### Phase E (2026-09-18) — daemon stage testable with no extra work

Tests: `Highlighting/RimworldXmlHighlightingTests.cs`, data `test/data/Highlighting/`.

**Steps 16–17 ✅, first run.** `HighlightingTestBase` (namespace `JetBrains.ReSharper.FeaturesTestFramework.Daemon`)
only needs `CompilerIdsLanguage => XmlLanguage.Instance` on top of the usual path/reference overrides. The gold is the
source with `|range|(n)` markers plus the list of highlightings: an invalid `bool` produces
`ReSharper Underlined Error Highlighting: Value must be "true" or "false"` on the right range, a valid one nothing.
With no Krafs reference the stage produces nothing and logs nothing.

**Fragility worth knowing:** `CustomXmlAnalysisStageProcess` never calls `ScopeHelper.UpdateScopes`; it reads
`ScopeHelper.RimworldScope` directly. It works because the stage is declared after `CollectUsagesStage`, which resolves
references, and our reference factory calls `UpdateScopes`. Reorder the stages or change the reference factory and the
analyser silently goes quiet.

### Phase E, expanded (2026-09-20) — the rest of the value validators

`Highlighting/RimworldXmlHighlightingTests.cs` covers the whole switch in three tests: `TestValidValues` and
`TestInvalidValues` over two inputs with the **same structure**, and `TestWithoutRimworld`. Between them the pair holds
every validating branch, both operands of each range, the types the stage deliberately skips (string,
`IntVec2`/`IntVec3`, `Color`, `Nullable<>`), the single-value shorthand the ranges and `Vector2` allow, values whose
field resolves to no type at all (unknown def type, misspelled field, RimWorld's `statBases` syntax, unknown
`li Class`) and whitespace-padded values. A value reported in one input and not in its twin is the difference between
them.

Two gaps turned up:

- **Floats are checked in a real mod but not in a C# project** (boundary 8). `TestInvalidValues` carries the float
  fields along with everything else and runs in the XML layout only, because in the C# one those values go unreported.

  `GetContextFromHierachy` identifies a field's type by `field.Type.GetLongPresentableName(CSharpLanguage.Instance)` and
  looks that string up in the symbol scope. What that returns depends on whether the type resolves in the module the
  file belongs to — `DeclaredTypeBase.GetPresentableName` is:

  ```csharp
  var typeElement = GetTypeElement();
  if (typeElement == null) return GetUnresolvedPresentation(...);   // "System.Single"
  return typePresenter.GetPresentableName(this, style);             // "float"
  ```

  Resolved, it reaches `CSharpTypePresenter`, whose default style carries `UseKeywordsForPredefinedTypes`, so a
  predefined type prints as its keyword — not a CLR name, resolves to nothing. Unresolved, it reaches
  `DeclaredTypeFromReflectionClassType.GetUnresolvedPresentation`, which prints `myClrTypeName.FullName` because
  `GetLongPresentableName` asks for `DefaultWithQualifiedName` — and that *does* resolve.

  In the shell the `.xml` is a file of the C# test project, `System.Single` resolves through its mscorlib, and floats go
  unchecked. In a mod, `Defs` belong to the plugin's own XML project, where it doesn't resolve, so the value is checked.
  Confirmed from both ends: the author moved the same file between the two projects in a real solution and watched the
  highlightings appear and disappear. Float validation therefore works by accident and only outside a C# project;
  `bool`/`string`/`int` work everywhere only because of the hand-written keyword switch, and `Verse.FloatRange` has no
  keyword to print so it behaves the same either way. Ruled out along the way, each by experiment: the Krafs reference
  assemblies (the real `Assembly-CSharp.dll` from a Steam install behaves the same), the filtered reference set (every
  framework DLL from the package changes nothing) and the compilation-context cookie (no cookie, the universal context
  and the file's own resolve context all answer `float`).

  **The fix is to read the CLR name off `field.Type` instead of its presentation**, which removes the fork entirely
  (adding `case "float"` only patches one keyword of a dozen). It was tried and reverted: it changes what completion
  and references resolve too, which the current tests don't cover, and `Nullable<>` has to be looked through at the
  same time or every nullable value gets reported as wrong. Left for a bug-fix pass.
- **Leading whitespace silences a tag** (boundary 9). Trailing whitespace is fine — it is included in both the checked
  text and the highlighted range — but a value written on its own line has a whitespace token before the text token, so
  `element.Parent.Children().FirstOrDefault(x => x is XmlFloatingTextToken) != element` sends the stage home. That is
  also why `ProcessBoolean`/`ProcessEnum` get away with comparing untrimmed text.

One more quirk, harmless but worth knowing: `Vector2`'s regex is lazy and `float.TryParse` accepts a thousands
separator, so `(1,2,3)` in a `Vector2` field parses its second component as `"2,3"` → 23 and reports nothing.

### Both project layouts (2026-09-20) — the harness can now build what a mod actually looks like

A mod keeps its `Defs` in the plugin's own XML project, not in a C# project, and that changes what the plugin sees
(boundary 8). A fixture now declares which layouts it runs in, and one class covers both:

```csharp
[ProjectLayouts(ProjectLayout.XmlProject, ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldXmlHighlightingTests(ProjectLayout layout) : RimworldHighlightingTestBase(layout)
```

`ProjectLayoutsAttribute` is an `IFixtureBuilder2` that builds one fixture per layout listed, so every `[Test]` in the
class runs once in each — against **one shared gold**. A test that passes in only one layout therefore *fails*, which
is the point. `--filter FullyQualifiedName~XmlProject` runs a single layout, which is how to regenerate a gold since
both layouts write the same `.tmp`.

**Declaring a layout is mandatory** for every fixture that touches the plugin: the layout-aware bases take it as a
constructor argument with no default, so a fixture without the attribute has nothing to build it from. Each fixture
therefore says which model it is testing, and the ones that only work in one say so, with a comment explaining what
fails in the other. (The smoke tests stay on the SDK's own bases — they exist to answer "do tests work at all".)

A test can narrow itself further: the same attribute on a `[Test]` method limits that test to the layouts it lists and
skips it in the others, so one awkward test doesn't hold its whole suite back (`TestInvalidValues` is the current
case).

| Suite | Layouts | Why |
|---|---|---|
| `RimworldXmlHighlightingTests` | both | `TestInvalidValues` narrowed to the XML layout (boundary 8) |
| `RimworldSymbolScopeTests` | both | |
| `AcceptCompletion.RimworldXmlTests` | both | |
| `RimworldXmlCompletionTests` | C# only | keyword-less type column and empty def-name lists in the XML layout |
| `RimworldNavigationTests` | C# only | nothing to navigate to in the XML layout; not investigated |
| `RimworldFindUsagesFromXmlTests` | C# only | no usages found in the XML layout; not investigated |
| `RimworldReferenceTests` | C# only | its one `.cs`-driven test lands in the referenceless project |
| `RimworldCSharpCompletionTests`, `RimworldFindUsagesFromCSharpTests`, `…WithoutRimworldTests` | C# only | the layout isn't what they're about |

Two things to know when adding a suite to the XML layout:

- The C# project has to hold *something*, which is `test/data/ModAssembly.cs` — one file for every suite, reached with
  the `..\` prefix the split computes from `RelativeTestDataPath`. Don't put copies in the suite folders: a completion
  fixture picks up stray `.cs` files in its own data folder, and a spare one changes what its tests see.
- The layout bases set the project names in their constructor and override `CanReuseSolution` to check them, because
  the framework otherwise hands a fixture a solution another fixture built in the other layout.

`ProjectLayout.cs` holds the enum, the attribute, the `IProjectLayoutFixture` marker and `ProjectLayoutSupport`, which
has all the behaviour. Changing an existing fixture's layouts is the attribute and nothing else. The only plumbing left
is per **SDK test class**, because C# has no mixins: a layout-aware base has to exist for each of them, and the four
that do live together in `TestBases/` (`RimworldHighlightingTestBase`, `RimworldCompletionTestBase`,
`RimworldNavigationTestBase`, `RimworldSolutionTestBase`). They are identical apart from which SDK class they extend — a constructor, `Layout`,
`ReferenceRimworld`, and four one-line overrides that hand off to `ProjectLayoutSupport`:

| Override | Hands off to |
|---|---|
| constructor | `ProjectNames` — names the two projects before the solution is built |
| `CanReuseSolution` | `CanReuse` — the framework otherwise hands over a solution built in the other layout |
| `GetReferencedAssemblies` | `ReferencedAssemblies` — the game's assemblies, unless `ReferenceRimworld` is off |
| `CreateProjectDescriptor` | `Libraries` — strips every reference off the XML project |
| `DoNamedTest` (or `DoLayoutTestSolution`) | `BuildSolution` — `.cs` data to the C# project, the Defs to the XML one |

Resetting `ScopeHelper` around each test, and skipping a test whose own `[ProjectLayouts]` excludes the fixture's
layout, are both done by the attribute (it is an NUnit `ITestAction`), so a base needs no `SetUp` for either. To cover
an SDK test class that has no base yet, copy one of the four and change what it extends.

The highlighting suite reproduces the real behaviour exactly: every invalid value in `TestInvalidValues` is reported
in the XML layout, and the float ones are not in the C# project - which is what the author sees when moving the same
file between the two projects in a real solution. Since the two layouts share a gold, that test carries
`[ProjectLayouts(XmlProject)]` and skips in the C# one until boundary 8 is fixed. `TestValidValues` runs in both.

That also settled the other open question: the whitespace-padded values behave the same in both layouts, so **the
whitespace gap (boundary 9) is real plugin behaviour, not a harness artifact.**

XML completion declares the C# layout only. Running it in both was tried and surfaces two differences, neither fixed:

1. The popup's type column loses its C# keywords — `Int32`, `Boolean`, `Single`, `List` instead of `int`, `bool`,
   `float`, `List<AltitudeLayer>`. Same cause as the float highlighting, cosmetic but user-visible.
2. Def-name completion comes back empty (`Count: 0`) in the emulated layout. It tracks the reference stripping — put the
   XML project's references back and the list fills — and everything the provider needs still works when probed
   directly (the def index finds both defs, `GetTagByDef` returns live nodes, the type resolves and its supertypes
   include `Verse.Def`). A real XML-only mod completing def names is the plugin's headline feature, so treat this as the
   emulation being harsher than the real project model until someone confirms otherwise in a real mod.

Next step for #2: give the XML project the project properties the real host uses (`RimworldXmlProjectHost` builds
`ProjectLanguage.JAVASCRIPT` with a `NetFramework` target framework of *null version*) instead of stripping a C#
project bare. `GetProjectProperties` is virtual but doesn't know which project it's building, so it needs a field set
from `CreateProjectDescriptor`. Once that behaves, turn the completion suite on with the attribute and gold both
layouts.

### Phase F (2026-09-18) — Find Usages runs; C# usages are missed, cause found

Tests: `FindUsages/RimworldFindUsagesTests.cs`, data `test/data/FindUsages/`. Same `AllNavigationProvidersTestBase` as
step 15; XML fixture and C# fixture (`[TestFileExtension]` differs) share an abstract base.

**Step 18 ⚠️.** Starting Find Usages from a `<defName>`, from a `Name=""` attribute or from a C# `GetNamed("…")` string,
every XML usage across files is found (including `ParentName=""`) and a same-named def of another type is correctly
excluded. **Usages in C# (the `[DefOf]` field, the `DefDatabase` string) are never listed** — even when the search
starts from that C# string. This is the gap `Find Usages.md` anticipates. Cause, verified with a temporary change:
`RimworldSearcherFactory.IsCompatibleWithLanguage` accepts only `XmlLanguage`, and the platform only hands a
searcher files in languages it accepts; letting it also accept `CSharpLanguage` makes both C# usages appear (5 results
instead of 3). `HasReference` on the C# reference factory is never consulted, so it isn't the blocker. Caveat before
shipping that one-liner: `CreateReferenceSearcher` filters *elements* through the same method, so it would also start
receiving C# declared elements. The golds record current behaviour and say so in the fixture's doc comment.

## Conclusions

Suite at the close: **25 green, 4 `[Ignore]`d** (each with a hand-written expected gold and a reason pointing here),
~12 s of tests after a ~1 min build.

**What the harness can test (proven):** XML and C# completion lists and insertion; the def index across files and
mixed C#/XML projects; reference resolution (dump); real IDE navigation — Go to Declaration into decompiled game
source, Find/Show/Highlight Usages; daemon highlightings. Every SDK test base tried worked on the `net10.0-windows`
host once its abstract members were supplied — no new harness fixes were needed after the proof of concept.

**Boundaries found — all in the plugin, none in the harness:**

| # | Problem | Where | Blocks |
|---|---|---|---|
| 1 | ✅ Fixed — Index reads PSI mid-commit ("uncommitted document" logged) | `RimworldSymbolScope.AddToLocalCache` via `Merge` | any test that edits an XML document (Action completion, future quick-fix/generator tests) |
| 2 | ✅ Fixed — Custom def subclasses not indexed under their base type on cold load | `ExtraDefTagNames` built only if `ScopeHelper.RimworldScope` is set at merge | step 9c |
| 3 | `[DefOf]` completion needs a following `;` | `CSharpDefsOfItemProvider.IsAvailable` (unfinished declaration parses as a method) | step 10 variant |
| 4 | Find Usages ignores C# | `RimworldSearcherFactory.IsCompatibleWithLanguage` | step 18's C# usages |
| 5 | `GetScopeForClass` searches `knownCustomScopes` twice (should be `allScopes`) | `ScopeHelper` | found by reading; needs a two-project test |
| 6 | Closing tags resolve only by coincidence | `GetHierarchy` on closing identifiers | cosmetic |
| 7 | Daemon stage relies on another stage having set the scope | `CustomXmlAnalysisStageProcess` | nothing yet; fragile |
| 8 | Predefined field types are identified by a *presented* name, which differs between a mod's XML project and a C# project, so floats are validated in a real mod but not in a C# project. Reading the CLR name off the type fixes it (tried, reverted — it reaches further than the highlighting tests cover; left for a bug-fix pass, and `Nullable<>` needs looking through at the same time or every nullable value is reported as wrong) | `GetContextFromHierachy`'s `GetLongPresentableName` + the `bool`/`string`/`int` keyword switch | `TestInvalidValues` runs in the XML layout only; any future test over a `float`, `double`, `long`, … field |
| 9 | ✅ Confirmed in both layouts — a value with leading whitespace (typically one written on its own line) is never validated | the "first text child" guard in `CustomXmlAnalysisStageProcess.ProcessAfterInterior` — the whitespace is a separate, earlier token | any multi-line value |

**Not explored:** Phase G (disk discovery via `RimworldPath`/`AddRef`, the generator) and Phase H (Rider-only code,
Remodder, Kotlin). #1 (now fixed) was the blocker for testing the generator (step 20). Also still open from the proof of
concept: moving CI to Windows and the `.gitattributes` LF rule for golds.

