# 23 · Where to ask JetBrains

**[Reference]** — *When something is upstream's problem, here's where to take it.*

A Rider plugin author talks to JetBrains regularly. The matrix below maps "thing I'm stuck on" to "where to ask."

## Slack

**JetBrains Platform Slack** — sign up at <https://plugins.jetbrains.com/slack>

Useful channels:
- `#intellij-platform` — general IntelliJ Platform questions, often the right starting point
- `#intellij-platform-rider` — Rider-specific (closer to this plugin's domain)
- `#intellij-platform-resharper` — ReSharper-specific (relevant for the Wave/csproj pattern)
- `#intellij-platform-gradle-plugin` — IPGP-specific bug reports and version-bump questions

JetBrains engineers are active here. Response times vary, but the signal-to-noise ratio is high. Best for "is this expected?", "did I miss a doc?", "can someone sanity-check this?"

## YouTrack (issue tracker)

- **Rider**: <https://youtrack.jetbrains.com/issues/RIDER>
- **ReSharper**: <https://youtrack.jetbrains.com/issues/RSRP>
- **IntelliJ IDEA platform**: <https://youtrack.jetbrains.com/issues/IDEA>
- **rd / RdFramework**: there's no dedicated project; cross-list under RIDER

For each, search before filing — often the issue is already tracked.

When filing:
- Title format: short and specific
- Body: include `gradle.properties` snippet, `build.gradle.kts` snippet, exact stack traces, and the IDE/SDK version
- Tag with version and platform if relevant

## GitHub repositories

- **`JetBrains/intellij-platform-gradle-plugin`** — <https://github.com/JetBrains/intellij-platform-gradle-plugin>
  - Issues for IPGP bugs and questions
  - Releases for the changelog (consult before bumping)
- **`JetBrains/rd`** — <https://github.com/JetBrains/rd>
  - rdgen / RdFramework
  - Releases for rd-gen versions
- **`JetBrains/resharper-unity`** — <https://github.com/JetBrains/resharper-unity>
  - The closest analog plugin to this one (cross-tier, RD-based)
  - When you can't figure out a Rider-plugin pattern, look here for prior art
  - Issues are reasonable to file/comment on if your problem touches Unity-style integration
- **`JetBrains/rider-plugin-template`** — <https://github.com/JetBrains/rider-plugin-template>
  - The official Rider plugin template — mostly what this repo derived from
  - Compare its `build.gradle.kts` against this repo's when something is broken; differences usually indicate drift

## Documentation

- **IntelliJ Platform plugin SDK home**: <https://plugins.jetbrains.com/docs/intellij/welcome.html>
- **Rider plugin development**: <https://plugins.jetbrains.com/docs/intellij/rider.html>
- **ReSharper plugin development (the canonical docs)**: <https://www.jetbrains.com/help/resharper/sdk/>
- **IPGP docs**: <https://plugins.jetbrains.com/docs/intellij/tools-intellij-platform-gradle-plugin.html>
- **Plugin verifier**: <https://plugins.jetbrains.com/docs/intellij/verifying-plugin-compatibility.html>
- **Marketplace publishing**: <https://plugins.jetbrains.com/docs/marketplace/>

The IntelliJ Platform docs are quite good for IntelliJ but Rider-specific guidance is sparse. When the canonical docs don't help, fall back to:
1. Read `JetBrains/resharper-unity` source for an existing pattern
2. Read `JetBrains/rider-plugin-template` for the canonical structure
3. Ask in `#intellij-platform-rider` Slack
4. File a YouTrack issue under RIDER

## Specific known-issue routing

For each entry in §17 (quirks and known issues) where the root cause is upstream:

| Issue | Where to file/ask |
|---|---|
| Mac/Linux DotFiles missing from Rider Maven artifact | YouTrack RIDER (search "DotFiles Maven Rider") |
| IPGP bundled-plugin → bundled-module reclassification surprises on upgrade | IPGP GitHub issues, or `#intellij-platform-gradle-plugin` Slack |
| `useBinaryReleases` flag rename or removal | IPGP GitHub release notes, or `#intellij-platform-gradle-plugin` Slack |
| RdFramework hash mismatches at runtime | rd GitHub issues, or `#intellij-platform-rider` Slack |
| Plugin verifier false-positive (e.g. "TemplateWordInPluginId") | IPGP GitHub issues |

## What NOT to ask JetBrains

- "How do I bump my plugin?" — that's in this wiki (§19) and the IPGP changelog
- "Does this Rimworld API exist?" — that's a Rimworld question, not a JetBrains one
- "Why does my code not compile?" — debug it locally first; bring a minimal reproduction if needed

## What's reasonable to ask

- "Is the Mac/Linux Maven artifact missing the Unity DotFiles by design?"
- "What's the recommended way to test a Rider plugin end-to-end?"
- "Has the bundled-plugin id for X changed in 2026.2?"
- "Is `provider { }` around `untilBuild` the recommended way to clear it?"
- "What's the right way to declare a custom Configuration that publishes a single SDK file?" (Even though §13 explains it, confirming with JetBrains is worthwhile.)

## Internal coordination

If you find that this wiki documents a pattern you needed, but JetBrains' own docs don't, *consider* opening a doc-improvement PR against the IntelliJ Platform docs (<https://github.com/JetBrains/intellij-sdk-docs>). The community has historically been the best source of Rider-plugin lore; sharing back lifts everyone.

→ Next: [24 · Refactor opportunities](24-refactor-opportunities.md)
