using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.CompletionSuggestions;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.AcceptCompletion;

/// <summary>
/// Gold is the document after accepting the item named by the input's ${COMPLETE_ITEM:…} directive.
/// Accepting an item commits the edited document, which runs RimworldSymbolScope.Merge mid-commit; these guard that the
/// index doesn't read PSI there (docs/testing-plan.md step 5).
/// </summary>
[ProjectLayouts(ProjectLayout.XmlProject, ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldXmlTests(ProjectLayout layout) : RimworldCompletionTestBase(layout)
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
    protected override string RelativeTestDataPath => @"AcceptCompletion\Rimworld";

    [Test] public void TestCompleteTag() => DoNamedTest();
    [Test] public void TestCompleteEnumValue() => DoNamedTest();
}