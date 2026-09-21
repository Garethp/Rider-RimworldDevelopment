using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.Highlighting;

[ProjectLayouts(ProjectLayout.XmlProject, ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldXmlHighlightingTests(ProjectLayout layout) : RimworldHighlightingTestBase(layout)
{
    protected override string RelativeTestDataPath => @"Highlighting";

    [Test] public void TestValidValues() => DoNamedTest();

    /// <summary>
    /// Float values are only reported when loaded up XmlProjects, they don't actually get matched properly in CSharp
    /// projects
    /// </summary>
    [ProjectLayouts(ProjectLayout.XmlProject)]
    [Test] public void TestInvalidValues() => DoNamedTest();
}

[ProjectLayouts(ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldXmlHighlightingWithoutRimworldTests(ProjectLayout layout) : RimworldHighlightingTestBase(layout)
{
    protected override string RelativeTestDataPath => @"Highlighting";
    protected override bool ReferenceRimworld => false;

    [Test] public void TestWithoutRimworld() => DoNamedTest();
}
