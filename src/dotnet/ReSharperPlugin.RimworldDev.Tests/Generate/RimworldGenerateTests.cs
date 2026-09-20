using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.Generate;

/// <summary>
/// Alt+Insert on a def. Gold is the properties the menu offers, in the order it offers them, followed by the document
/// after generating the ones the input selects.
/// </summary>
[ProjectLayouts(ProjectLayout.XmlProject, ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldGenerateTests(ProjectLayout layout) : RimworldGenerateTestBase(layout)
{
    protected override string RelativeTestDataPath => @"Generate";

    // The dump spells out generic type arguments only where they resolve, which is not in a mod's own project
    [ProjectLayouts(ProjectLayout.XmlProject)]
    [Test] public void TestGenerateProperties() => DoNamedTest();

    [Test] public void TestGenerateInListItem() => DoNamedTest();
}
