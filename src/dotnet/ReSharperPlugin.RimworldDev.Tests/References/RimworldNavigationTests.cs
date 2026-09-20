using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.IntentionsTests.Navigation;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.CompletionSuggestions;

namespace ReSharperPlugin.RimworldDev.Tests.References;

/// <summary>
/// Ctrl+Click the way the IDE does it: every context navigation provider (Go to Declaration, Find Usages, …) is run at
/// {caret} and the targets it would offer are dumped.
/// </summary>
[TestFileExtension(".xml")]
public class RimworldNavigationTests : AllNavigationProvidersTestBase
{
    protected override string ExtraPath => "";
    protected override string RelativeTestDataPath => @"References\Navigation";

    protected override IEnumerable<string> GetReferencedAssemblies(TargetFrameworkId targetFrameworkId) =>
        base.GetReferencedAssemblies(targetFrameworkId).Concat(RimworldCompletionTestBase.RimworldReferenceAssemblies());

    [SetUp]
    public void ResetRimworldScope()
    {
        ScopeHelper.Reset();
        ScopeHelper.SkipAssemblyDiscovery = true;
    }

    [TearDown]
    public void ForgetRimworldScope() => ScopeHelper.Reset();

    [Test] public void TestNavigateToCSharpField() => DoNamedTest();
    [Test] public void TestNavigateToXmlDef() => DoNamedTest();
}
