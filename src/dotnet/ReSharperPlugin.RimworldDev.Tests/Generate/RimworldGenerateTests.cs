using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.Generate;

/// <summary>
/// This tests the `Alt+Insert` Generation menu inside a Def.
/// </summary>
[ProjectLayouts(ProjectLayout.XmlProject, ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldGenerateTests(ProjectLayout layout) : RimworldGenerateTestBase(layout)
{
    protected override string RelativeTestDataPath => @"Generate";

    [ProjectLayouts(ProjectLayout.XmlProject)]
    [Test] public void TestGenerateProperties() => DoNamedTest();

    [Test] public void TestGenerateInListItem() => DoNamedTest();
}
