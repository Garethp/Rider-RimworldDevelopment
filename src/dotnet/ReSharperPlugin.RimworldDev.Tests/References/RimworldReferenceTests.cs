using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.References;

[ProjectLayouts(ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldReferencesFromXmlTests(ProjectLayout layout) : RimworldReferenceTestBase(layout)
{
    protected override string RelativeTestDataPath => @"References";

    [Test] public void TestXmlToCSharp() => DoNamedTest();
    [Test] public void TestXmlToXmlDef() => DoNamedTest("OtherDefs.xml");
}

[ProjectLayouts(ProjectLayout.CSharpProject)]
[TestFileExtension(".cs")]
public class RimworldReferencesFromCSharpTests(ProjectLayout layout) : RimworldReferenceTestBase(layout)
{
    protected override string RelativeTestDataPath => @"References";

    [Test] public void TestCSharpToXmlDef() => DoNamedTest("CSharpDefs.xml");
}
