using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.References;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.FindUsages;

/// <summary>
/// Find Usages (and the other navigation providers) started from a def, with usages spread across XML and C# files.
/// XMLTagDeclaredElement + RimworldSearcherFactory/CustomSearcher is the known rough edge; see "Find Usages.md".
/// The golds record *current* behaviour: usages in XML are found, usages in C# (the [DefOf] field and the
/// DefDatabase string in CSharpUsages.cs) are not, because RimworldSearcherFactory.IsCompatibleWithLanguage only
/// accepts XML. Allowing C# there makes both appear (verified), so when that's fixed these golds should gain them.
/// </summary>
[ProjectLayouts(ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldFindUsagesFromXmlTests(ProjectLayout layout) : RimworldNavigationTestBase(layout)
{
    protected override string RelativeTestDataPath => "FindUsages";
    
    [Test] public void TestFromDefName() => DoNamedTest("OtherUsages.xml", "CSharpUsages.cs");
    [Test] public void TestFromNameAttribute() => DoNamedTest();
}

[ProjectLayouts(ProjectLayout.CSharpProject)]
[TestFileExtension(".cs")]
public class RimworldFindUsagesFromCSharpTests(ProjectLayout layout) : RimworldNavigationTestBase(layout)
{
    protected override string RelativeTestDataPath => "FindUsages";
    
    [Test] public void TestFromCSharpString() => DoNamedTest("Defs.xml", "OtherUsages.xml");
}
