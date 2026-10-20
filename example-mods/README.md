# Highlighting fixtures

Two mods that differ in one thing: **which project the `Defs` XML belongs to**.

| Mod | Arrangement |
|---|---|
| `XmlOnlyMod` | No csproj — the mod is picked up from `About/About.xml` by the plugin's own XML project model |
| `CsprojMod` | A csproj at the **mod root**, so `Defs/*.xml` are files of the C# project |

They were built to settle why the same def file is validated differently in the two (boundary 8 in
`docs/testing-plan.md`). **That question is answered**, and the backend tests now cover both layouts themselves —
`RimworldXmlHighlightingTests` carries `[ProjectLayouts(XmlProject, CSharpProject)]` and runs every test in both
against one gold, so a difference between them fails. The whitespace question is answered too: it passes in both, so
that gap is real plugin behaviour.

The mods are still here for eyeballing the real IDE, and there is one open question they can answer:

## Open: does def-name completion work in an XML-only mod?

Emulating the XML project in the test shell by stripping every reference off it leaves def-name completion empty
(`Count: 0`), while the same test with references in place fills the list. Nothing says a real mod behaves that way —
it's the plugin's headline feature — so the emulation is probably harsher than the real project model.

To check: open `XmlOnlyMod`, put the caret inside `<minifiedDef></minifiedDef>` in a def and see whether `TestThingA`
and the other defs are offered.

```powershell
./gradlew runIde --args=XmlOnlyMod
./gradlew runIde --args=CsprojMod
```

(`--args` takes a folder name under `example-mods/` or any path; with none, `runIde` opens `AshAndDust`. File → Open inside the
sandbox works just as well.)

If it completes there, the emulation needs the real project's properties (`ProjectLanguage.JAVASCRIPT` and a
`NetFramework` target framework with a null version, as `RimworldXmlProjectHost` builds) rather than a stripped C#
project. If it doesn't complete, that's a real bug and worth its own test.

## The two files in each mod

- `Defs/FloatValues.xml` — six invalid values; a real install reports all six, a C# project reports two.
- `Defs/WhitespaceValues.xml` — five invalid values; only the trailing-whitespace one is reported, in both layouts.
