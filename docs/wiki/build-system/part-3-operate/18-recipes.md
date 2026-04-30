# 18 · Recipes

**[Operate / Recipes]** — *Day-to-day "how do I do X?" answers. Bookmark this page.*

Each recipe is short and copy-pasteable. If a recipe needs context, it links back to the Part 2 page that explains why.

## Run the plugin locally

```bash
./gradlew runIde
```

Prerequisites:
- JDK 21 on PATH (Gradle's toolchain will auto-provision if missing)
- .NET SDK 7+ on PATH (`global.json` pins 7.0.202 with `rollForward: latestMajor`)
- ~10 GB free disk (Rider SDK + sandbox)
- Internet on first run (SDK download)
- **On Mac/Linux**: Rider installed locally via JetBrains Toolbox, `/opt/rider`, `/usr/share/rider`, or `/Applications/Rider.app`. Otherwise the DotFiles patcher silently no-ops and the rider-unity plugin fails to load. See §14.

A clean first run takes 5–15 minutes (mostly Rider SDK download). Subsequent runs ~30–60 seconds.

## Iterate on Kotlin only (skip .NET rebuild)

```bash
./gradlew runIde -x compileDotNet
```

Half the iteration time. The `-x` flag tells Gradle to exclude a task. Valid as long as you haven't touched `.cs` files.

## Bump the plugin version

1. Edit `gradle.properties:7`:
   ```properties
   PluginVersion=2025.1.11
   ```
2. Add a new `## 2025.1.11` section at the **top** of `CHANGELOG.md`. The `tasks.patchPluginXml` block parses the first `##`-headed section into the plugin's "what's new" HTML.
3. (Optional) The `<version>` in `plugin.xml` is overridden by `patchPluginXml` at build time, so you don't need to edit it.

Test: `./gradlew :buildPlugin` — output filename should reflect the new version.

## Add a .NET package reference

1. Edit `src/dotnet/ReSharperPlugin.RimworldDev/ReSharperPlugin.RimworldDev.Rider.csproj` (the Rider flavour, where Remodder dependencies live):
   ```xml
   <PackageReference Include="YourPackage" Version="1.2.3" />
   ```
2. If the package needs to be in the Wave/ReSharper flavour too, edit `ReSharperPlugin.RimworldDev.csproj` instead (or in addition).
3. If the package ships a runtime DLL that must end up in the plugin (rather than provided by Rider), add the DLL filename to `prepareSandbox`'s `dllFiles` list at `build.gradle.kts:160-171`:
   ```kotlin
   "$outputFolder/YourPackage.dll",
   ```
4. Test: `./gradlew compileDotNet` to verify the .csproj builds, then `./gradlew :buildPlugin` to verify the DLL ends up in the plugin ZIP.

(Yes, manually adding the DLL filename is fragile. §17 + §24 capture this.)

## Add a new RPC between frontend and backend

1. Edit `protocol/src/main/kotlin/model/rider/Model.kt`. Add a `call(...)`, `signal(...)`, or `property(...)` inside `init { }`:
   ```kotlin
   call("doThing", string, int).async
   ```
2. Run `./gradlew :protocol:rdgen`.
3. Verify the regenerated files appeared with new symbols:
   - `src/rider/main/kotlin/remodder/RemodderProtocolModel.Generated.kt`
   - `src/dotnet/ReSharperPlugin.RimworldDev/RemodderProtocolModel.Generated.cs`
4. Implement the call site (frontend Kotlin) and the handler (backend C#).
5. Commit the regenerated `*.Generated.*` files alongside your protocol edit.

See §10 for how rdgen and the protocol DSL work.

## Bump the Rider SDK (e.g. 2026.1 → 2026.2)

This requires editing several places. See §19 for the full runbook. Quick version: edit `gradle.properties` (`ProductVersion`, `rdVersion`), `Directory.Build.props` (`SdkVersion`), `gradle/libs.versions.toml` (`rdGen` patch). Possibly also `rdKotlinVersion` and `kotlin` if the bundled Kotlin shifts.

## Publish a release

1. Make sure `gradle.properties:7 PluginVersion` matches the version you're about to tag (cosmetic — `Deploy.yml` overrides it via `-PPluginVersion="${{ github.ref_name }}"`).
2. Update `CHANGELOG.md` with the new section.
3. Push a Git tag matching `*.*.*`:
   ```bash
   git tag 2025.1.11
   git push origin 2025.1.11
   ```
4. `Deploy.yml` runs automatically: publishes the Rider plugin to Marketplace, the ReSharper plugin to Marketplace, and attaches both to a GitHub release.

Prerequisites:
- The `PUBLISH_TOKEN` repo secret must be set with a valid JetBrains Marketplace token.

## Run the .NET tests

```bash
./gradlew testDotNet
```

Currently a near-no-op (the Tests project has no `.cs` files yet). When tests are added, this is the gate that runs them.

Equivalent direct invocation: `dotnet test ReSharperPlugin.RimworldDev.sln`.

## Build the ReSharper-only nupkg locally

```bash
./gradlew buildResharperPlugin
```

Output lands at `output/ReSharperPlugin.RimworldDev.<version>.nupkg`. Useful for testing the ReSharper-for-VS flavour without going through the publish flow.

## Skip the IntelliJ plugin verifier

It only runs on `:verifyPlugin`, which CI doesn't currently invoke. Don't run it. (To run it: `./gradlew verifyPlugin`.)

## Override a Gradle property at the command line

```bash
./gradlew buildPlugin -PPluginVersion=2025.1.99
```

The `-P<name>=<value>` flag overrides any `gradle.properties` entry of the same name. Used by CI and by anyone who needs to test with a non-default value without committing.

## Connect a debugger to the running plugin

See §15 for the full procedure.

Short version:
- **JVM frontend**: click the Debug button next to `runIde` in your dev IDE, or attach manually to the JVM process spawned by `./gradlew runIde`.
- **.NET backend**: in your dev Rider, *Run → Attach to Process…*, find the spawned `Rider.Backend` (or `dotnet`) process, attach. Set breakpoints in `src/dotnet/...`.

## Diagnose "File … does not exist" from prepareSandbox

The `prepareSandbox.doLast` block asserts each expected DLL exists in `bin/`. If the assertion fires:

1. Did `compileDotNet` actually run? `./gradlew compileDotNet --info` and check the output.
2. Is the DLL in the right `bin/` folder? Check `src/dotnet/ReSharperPlugin.RimworldDev/bin/ReSharperPlugin.RimworldDev.Rider/Release/`.
3. If the DLL is missing entirely, check whether the .csproj still includes it (e.g. did you remove a `PackageReference` recently?).
4. If the DLL is in a different folder, has the `BuildConfiguration` property changed?

See §14 for the full troubleshooting matrix.

## Regenerate the protocol bindings without building everything

```bash
./gradlew :protocol:rdgen
```

Runs only the rdgen task on the `:protocol` subproject. The regenerated `*.Generated.kt` and `*.Generated.cs` will appear; `git diff` will show what changed.

## Clean everything

```bash
./gradlew clean
```

Wipes `build/` directories. Doesn't touch the cached IDE downloads (those live in `~/.gradle/`) or `output/`. Use `git clean -fdx` if you need to nuke `output/` and other gitignored content too — be careful.

## Inspect the task graph

```bash
./gradlew runIde --dry-run
```

Prints what would run without doing it. Useful when you suspect a `dependsOn` is missing or wrong.

```bash
./gradlew :buildPlugin --info
./gradlew :buildPlugin --warning-mode all
./gradlew :buildPlugin --configuration-cache
```

Increasingly verbose modes for debugging build behaviour. `--configuration-cache` is the most stringent and surfaces any cache-hostile patterns.

## "I just want a release ZIP"

```bash
./gradlew :buildPlugin
```

Produces `build/distributions/rimworlddev-<version>.zip` and copies it to `output/` (the `tasks.buildPlugin { doLast }` post-action handles this).

→ Next: [19 · Upgrade runbooks](19-upgrade-runbooks.md)
