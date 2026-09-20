using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.CompletionSuggestions;

namespace ReSharperPlugin.RimworldDev.Tests.AcceptCompletion;

/// <summary>
/// Gold is the document after accepting the item named by the input's ${COMPLETE_ITEM:…} directive.
/// Accepting an item commits the edited document, which runs RimworldSymbolScope.Merge mid-commit; these guard that the
/// index doesn't read PSI there (docs/testing-plan.md step 5).
/// </summary>
[TestFileExtension(".xml")]
public class RimworldXmlTests : RimworldCompletionTestBase
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
    protected override string RelativeTestDataPath => @"AcceptCompletion\Rimworld";

    [Test] public void TestCompleteTag() => DoNamedTest();
    [Test] public void TestCompleteEnumValue() => DoNamedTest();
}