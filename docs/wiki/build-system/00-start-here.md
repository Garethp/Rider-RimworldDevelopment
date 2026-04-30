# Start here

This wiki teaches you how the build system for the **Rimworld Development Environment** Rider plugin works, end-to-end, from zero. By the end, you should be able to clone the repo, build it, run it, modify it, and confidently bump versions of Gradle / IntelliJ Platform / rdgen / Rider SDK / Kotlin / JDK without comparing against unrelated JetBrains template repos.

## What this is

A graduated learning journey, not a reference dump. Pages assume only what came before them. Read top to bottom on first pass; bookmark §18 (recipes) and §07 (version-pinning map) for daily use.

## Reading paths

| You are... | Read |
|---|---|
| New to JetBrains plugins **and** Gradle | All of it, in order |
| Comfortable with Gradle, new to JetBrains plugins | Skip §03–§04, start at §05 |
| Comfortable with IntelliJ plugins, new to Rider plugins | Skim §01–§02, focus on §10–§14 |
| Coming back to do one task | §07 + §18 + the relevant §19 runbook |

## What's in each part

- **Part 1 — Foundation** (§01–§05). Conceptual teaching only. What a Rider plugin is, the two-tier mental model, just-enough-Gradle, the cast of build tools.
- **Part 2 — This project** (§06–§17). Tied to actual file:line in this repo. Repo tour, version map, annotated `build.gradle.kts`, the `:protocol` subproject, the .NET side, dual-csproj pattern, the `riderModel` bridge, the `prepareSandbox` glue, runIde, CI/publish, and a quirks ledger.
- **Part 3 — Operate** (§18–§19). Day-to-day recipes and version-bump runbooks.
- **Part 4 — Reference** (§20–§24). Diagrams, a contributed-tasks table, a glossary, where to ask JetBrains for help, and a refactor backlog.

## Promise

Sections 1–3 plus the relevant recipe is sufficient for 80% of contributor tasks. The rest is for when something unusual happens or you want to upgrade the build itself.

## Audience tags

Most pages in Part 2 lead with one of:

- **This works the same as IntelliJ plugins; the only twist is X** — for readers from the IntelliJ side
- **This is unique to Rider plugins** — for the JVM↔.NET bridging story
- **This is a custom workaround in this repo, not standard** — for the local hacks

Watch for these tags. They tell you whether your existing intuition applies.
