using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.IntentionsTests.Navigation;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.CompletionSuggestions;

namespace ReSharperPlugin.RimworldDev.Tests.FindUsages;

/// <summary>
/// Find Usages (and the other navigation providers) started from a def, with usages spread across XML and C# files.
/// XMLTagDeclaredElement + RimworldSearcherFactory/CustomSearcher is the known rough edge; see "Find Usages.md".
/// The golds record *current* behaviour: usages in XML are found, usages in C# (the [DefOf] field and the
/// DefDatabase string in CSharpUsages.cs) are not, because RimworldSearcherFactory.IsCompatibleWithLanguage only
/// accepts XML. Allowing C# there makes both appear (verified), so when that's fixed these golds should gain them.
/// </summary>
public abstract class RimworldFindUsagesTestBase : AllNavigationProvidersTestBase
{
    protected override string ExtraPath => "";
    protected override string RelativeTestDataPath => @"FindUsages";

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
}

[TestFileExtension(".xml")]
public class RimworldFindUsagesFromXmlTests : RimworldFindUsagesTestBase
{
    [Test] public void TestFromDefName() => DoNamedTest("OtherUsages.xml", "CSharpUsages.cs");
    [Test] public void TestFromNameAttribute() => DoNamedTest();
}

[TestFileExtension(".cs")]
public class RimworldFindUsagesFromCSharpTests : RimworldFindUsagesTestBase
{
    [Test] public void TestFromCSharpString() => DoNamedTest("Defs.xml", "OtherUsages.xml");
}
