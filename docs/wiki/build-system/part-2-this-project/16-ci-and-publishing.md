# 16 · CI and publishing

**[This Project]** — *Standard GitHub Actions; the publishing flow is dual-artifact (Rider zip + ReSharper nupkg).*

Two workflows under `.github/workflows/`:

- `CI.yml` — runs on every push to `main` and every PR. Builds and tests.
- `Deploy.yml` — runs on tag pushes matching `*.*.*`. Publishes to the JetBrains Marketplace and attaches artifacts to a GitHub release.

Both run on `ubuntu-latest` runners, set up JDK 21 (Corretto, with Gradle cache) and .NET SDK 8, then call Gradle.

## CI.yml — what runs on every PR

`.github/workflows/CI.yml`:

Two jobs, both on `ubuntu-latest`:

### Build job

```yaml
- run: ./gradlew :buildPlugin --no-daemon
- run: ./gradlew :buildResharperPlugin --no-daemon
- uses: actions/upload-artifact@v4
  with:
    name: ${{ github.event.repository.name }}.CI.${{ github.head_ref || github.ref_name }}
    path: output
```

This produces:
- `build/distributions/rimworlddev-<version>.zip` — copied to `output/` by the `tasks.buildPlugin { doLast { ... } }` post-action (§09)
- `output/ReSharperPlugin.RimworldDev.<version>.nupkg` — produced directly by `buildResharperPlugin`'s `Pack` MSBuild target

The artifact upload captures everything in `output/` so reviewers can download the built plugin from the GitHub Actions run page.

`--no-daemon` matters because CI containers benefit from deterministic Gradle shutdown: no daemons left running, no leaked file locks, no surprise behavior on the next job.

### Test job

```yaml
- run: ./gradlew :testDotNet --no-daemon
```

Currently a near-no-op because `ReSharperPlugin.RimworldDev.Tests` has no `.cs` test files. The runner exits 0 with nothing to fail. When tests are added, this is the gate.

## Deploy.yml — what runs on a release tag

`.github/workflows/Deploy.yml`:

Trigger: push of a tag matching `*.*.*` (e.g. `2025.1.11`).

Single job with four publish steps:

### Step 1: Publish the Rider plugin

```yaml
- name: Publish Rider Package
  run: ./gradlew :publishPlugin -PBuildConfiguration="Release" -PPluginVersion="${{ github.ref_name }}" -PPublishToken="${{ secrets.PUBLISH_TOKEN }}"
```

This invokes IPGP's `publishPlugin` task. Looking at `build.gradle.kts:232-236`:

```kotlin
tasks.publishPlugin {
    dependsOn(testDotNet)
    dependsOn(tasks.buildPlugin)
    token.set(PublishToken)
}
```

Effective sequence:
1. `testDotNet` runs (currently no-op)
2. `buildPlugin` runs (produces the ZIP)
3. `publishPlugin` uploads the ZIP to JetBrains Marketplace under plugin id `com.jetbrains.rider.plugins.rimworlddev` (from `plugin.xml:2`)

The `-P` flags override `gradle.properties` defaults. `PluginVersion="${{ github.ref_name }}"` means the tag name itself is the version — so the tag `2025.1.11` becomes the released version. `PublishToken` overrides the `"_PLACEHOLDER_"` default with the real Marketplace token.

### Step 2: Build the ReSharper nupkg

```yaml
- run: ./gradlew :buildResharperPlugin
```

Produces `output/ReSharperPlugin.RimworldDev.<version>.nupkg`. Note: the `:buildResharperPlugin` task in `build.gradle.kts:92-105` doesn't include a `Pack` step that picks up `-PPluginVersion` automatically — but it interpolates `$PluginVersion` from the property at configuration time, so the `-P` override above does flow through.

### Step 3: Publish the ReSharper plugin

```yaml
- name: Publish ReSharper Package
  run: dotnet nuget push --source "https://plugins.jetbrains.com/api/v2/package" --api-key "$PUBLISH_TOKEN" output/ReSharperPlugin*.nupkg
```

This bypasses Gradle entirely — a direct `dotnet nuget push` to JetBrains' Marketplace package endpoint. The Marketplace accepts both `.zip` (Rider/IntelliJ plugins) and `.nupkg` (ReSharper plugins) at different upload paths; this is the ReSharper one.

The ReSharper plugin lands under id `ReSharperPlugin.RimworldDev` (the assembly name). **Different listing** from the Rider plugin: a Rider user installs `com.jetbrains.rider.plugins.rimworlddev`, a ReSharper-for-VS user installs `ReSharperPlugin.RimworldDev`. They're separately versioned, but typically released in lockstep.

### Step 4: Attach to GitHub release

```yaml
- name: Upload binaries to release
  run: gh release upload ${{ github.ref_name }} output/*
```

Both the `.zip` and the `.nupkg` get attached to the GitHub release for the tag, so users on air-gapped networks (or just looking at the GitHub release page) can grab them directly.

## What's NOT used by CI

The PowerShell scripts at the repo root:
- `buildPlugin.ps1` — duplicates `:buildResharperPlugin`, not invoked
- `publishPlugin.ps1` — duplicates the `dotnet nuget push` step, not invoked
- `settings.ps1` — sourced by the above two; not invoked
- `tools/vswhere.exe`, `tools/nuget.exe` — used by `settings.ps1`; not invoked

Of the four, only **`runVisualStudio.ps1`** has a unique role (set up an experimental ReSharper hive in Visual Studio for local ReSharper-for-VS development). The other three plus the `tools/` directory are deletion candidates (§24).

## The `-P` property override mechanism

Throughout `Deploy.yml` you see `-PPluginVersion=...`, `-PPublishToken=...`, `-PBuildConfiguration=...`. These override `gradle.properties` at the CLI level. The flow:

1. `gradle.properties:7 PluginVersion=2025.1.10` sets a default
2. CI invokes Gradle with `-PPluginVersion="${{ github.ref_name }}"` — Gradle's CLI `-P` flag overrides
3. `val PluginVersion: String by project` in `build.gradle.kts:31` reads the *effective* value (the override)
4. The override propagates to `tasks.patchPluginXml`, `tasks.buildResharperPlugin`, and `tasks.buildPlugin` filenames

This is how a tag push with `2025.1.11` produces a `rimworlddev-2025.1.11.zip` even though `gradle.properties` still says `2025.1.10`.

## Summary diagram

```
                Tag push (e.g. 2025.1.11)
                          │
                          ▼
                    Deploy.yml runs
                          │
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
  publishPlugin    buildResharperPlugin   GitHub Release
  (Gradle)         (Gradle)               (gh release upload)
        │                 │                 │
        ▼                 ▼                 │
   rimworlddev      ReSharperPlugin         │
   -2025.1.11.zip   .RimworldDev            │
        │           .2025.1.11.nupkg        │
        │                 │                 │
        ▼                 ▼                 │
   JetBrains       JetBrains          GitHub Release
   Marketplace     Marketplace        binaries attached
   (Rider plugin   (ReSharper plugin
    listing)        listing)
```

→ Next: [17 · Quirks and known issues](17-quirks-and-known-issues.md)
