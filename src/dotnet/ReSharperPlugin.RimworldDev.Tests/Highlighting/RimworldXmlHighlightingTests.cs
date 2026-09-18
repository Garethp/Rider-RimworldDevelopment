using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.Completion;

namespace ReSharperPlugin.RimworldDev.Tests.Highlighting;

/// <summary>
/// CustomXmlAnalysisStage: XML values checked against the C# type of the field they set. Gold is the source with the
/// highlighted ranges marked, followed by the list of highlightings.
/// </summary>
[TestFileExtension(".xml")]
public class RimworldXmlHighlightingTests : HighlightingTestBase
{
    protected override string RelativeTestDataPath => @"Highlighting";
    protected override PsiLanguageType CompilerIdsLanguage => XmlLanguage.Instance;

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

    [Test] public void TestInvalidBool() => DoNamedTest();
}

/// <summary>
/// The same input with no RimWorld types in the solution: the stage must quietly produce nothing (and log nothing).
/// </summary>
[TestFileExtension(".xml")]
public class RimworldXmlHighlightingWithoutRimworldTests : HighlightingTestBase
{
    protected override string RelativeTestDataPath => @"Highlighting";
    protected override PsiLanguageType CompilerIdsLanguage => XmlLanguage.Instance;

    [SetUp]
    public void ResetRimworldScope()
    {
        ScopeHelper.Reset();
        ScopeHelper.SkipAssemblyDiscovery = true;
    }

    [TearDown]
    public void ForgetRimworldScope() => ScopeHelper.Reset();

    [Test] public void TestNoRimworldReference() => DoNamedTest();
}
