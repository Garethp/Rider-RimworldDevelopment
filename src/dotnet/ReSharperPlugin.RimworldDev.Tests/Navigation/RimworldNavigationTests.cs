using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;
using System.Linq;

namespace ReSharperPlugin.RimworldDev.Tests.Navigation;

/// <summary>
/// These tests essentially emulate us doing a Ctrl+Click in the IDE. The gold files should show what options were
/// presented to the IDE (not the user) from what sources. The distinction is that the same reference/result may be
/// presented to the IDE form multiple sources and that'll be deduplicated to a single option for the IDE.
/// </summary>
[ProjectLayouts(ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldNavigationTests(ProjectLayout layout) : RimworldNavigationTestBase(layout)
{
    protected override string ExtraPath => "";
    protected override string RelativeTestDataPath => "Navigation";

    [Test] public void TestNavigatePropertyToCSharpField() => DoNamedTest();
    [Test] public void TestNavigateValueToXmlDef() => DoNamedTest();
    [Test] public void TestNavigateValueToCSharpEnum() => DoNamedTest();
}


