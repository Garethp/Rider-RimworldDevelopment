using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.References;

/// <summary>
/// Ctrl+Click the way the IDE does it: every context navigation provider (Go to Declaration, Find Usages, …) is run at
/// {caret} and the targets it would offer are dumped.
/// </summary>
[ProjectLayouts(ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldNavigationTests(ProjectLayout layout) : RimworldNavigationTestBase(layout)
{
    protected override string ExtraPath => "";
    protected override string RelativeTestDataPath => @"References\Navigation";

    [Test] public void TestNavigateToCSharpField() => DoNamedTest();
    [Test] public void TestNavigateToXmlDef() => DoNamedTest();
}
