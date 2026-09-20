using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.Highlighting;

/// <summary>
/// CustomXmlAnalysisStage: XML values checked against the C# type of the field they set. Gold is the source with the
/// highlighted ranges marked, followed by the list of highlightings. The two inputs have the same structure, so a
/// value that is reported in one and not the other is the difference between them.
/// </summary>
[ProjectLayouts(ProjectLayout.XmlProject, ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldXmlHighlightingTests(ProjectLayout layout) : RimworldHighlightingTestBase(layout)
{
    protected override string RelativeTestDataPath => @"Highlighting";

    [Test] public void TestValidValues() => DoNamedTest();

    // Float values are only reported in a mod's own project; see boundary 8 in docs/testing-plan.md
    [ProjectLayouts(ProjectLayout.XmlProject)]
    [Test] public void TestInvalidValues() => DoNamedTest();
}

/// <summary>
/// The same input with no RimWorld types in the solution: the stage must quietly produce nothing (and log nothing).
/// </summary>
[ProjectLayouts(ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldXmlHighlightingWithoutRimworldTests(ProjectLayout layout) : RimworldHighlightingTestBase(layout)
{
    protected override string RelativeTestDataPath => @"Highlighting";
    protected override bool ReferenceRimworld => false;

    [Test] public void TestWithoutRimworld() => DoNamedTest();
}
