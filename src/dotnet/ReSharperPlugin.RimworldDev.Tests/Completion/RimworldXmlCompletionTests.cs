using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;

namespace ReSharperPlugin.RimworldDev.Tests.Completion;

/// <summary>
/// The real thing: RimWorld XML completion backed by the game's types. Krafs.Rimworld.Ref (a complete reference
/// assembly for RimWorld) is referenced into the in-memory project, and ScopeHelper finds it the same way it finds
/// the real Assembly-CSharp.dll: by looking for Verse.ThingDef.
/// </summary>
[TestFileExtension(".xml")]
public class RimworldXmlCompletionTests : CodeCompletionTestBase
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
    protected override string RelativeTestDataPath => @"Completion\Rimworld";

    /// <summary>
    /// Every DLL from the Krafs package's ref/net472 folder except the framework ones, which the test platform
    /// already provides. The folder path is baked into this assembly by the csproj from NuGet's restore.
    /// </summary>
    protected override IEnumerable<string> GetReferencedAssemblies(TargetFrameworkId targetFrameworkId)
    {
        var refDir = typeof(RimworldXmlCompletionTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "RimworldRefDir").Value;

        var rimworldDlls = Directory.GetFiles(refDir, "*.dll")
            .Where(path =>
            {
                var name = Path.GetFileName(path);
                return !name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) &&
                       !name.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                       !name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) &&
                       !name.StartsWith("Mono.", StringComparison.OrdinalIgnoreCase);
            });

        return base.GetReferencedAssemblies(targetFrameworkId).Concat(rimworldDlls);
    }

    [SetUp]
    public void ResetRimworldScope()
    {
        // ScopeHelper caches the RimWorld scope in statics; without this the second test reuses a scope from a solution
        // that no longer exists. The discovery switch stops it from finding a real RimWorld install on this machine and
        // adding that to the test solution on top of the Krafs reference.
        ScopeHelper.Reset();
        ScopeHelper.SkipAssemblyDiscovery = true;
    }

    [TearDown]
    public void ForgetRimworldScope()
    {
        // Drop our references to the solution's modules before the framework checks that nothing is still holding them.
        ScopeHelper.Reset();
    }

    [Test] public void TestThingDefProperties() => DoNamedTest();
}
