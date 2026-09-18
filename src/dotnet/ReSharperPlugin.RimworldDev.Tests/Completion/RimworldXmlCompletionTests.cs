using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace ReSharperPlugin.RimworldDev.Tests.Completion;

/// <summary>
/// The real thing: RimWorld XML completion backed by the game's types. Gold is the lookup list at {caret}.
/// </summary>
[TestFileExtension(".xml")]
public class RimworldXmlCompletionTests : RimworldCompletionTestBase
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
    protected override string RelativeTestDataPath => @"Completion\Rimworld";

    [Test] public void TestThingDefProperties() => DoNamedTest();
    [Test] public void TestNestedFieldProperties() => DoNamedTest();
    [Test] public void TestListItemProperties() => DoNamedTest();
    [Test] public void TestListItemWithClassProperties() => DoNamedTest();
    [Test] public void TestEnumValue() => DoNamedTest();

    // Phase B: the def index (RimworldSymbolScope)
    [Test] public void TestDefReferenceSameFile() => DoNamedTest();
    [Test] public void TestDefReferenceOtherFile() => DoNamedTest("OtherDefs.xml");
    [Test] public void TestParentName() => DoNamedTest();
    [Test] public void TestModDefClassProperties() => DoNamedTest("ModTypes.cs");
    [Test] public void TestModListItemClassProperties() => DoNamedTest("ModTypes.cs");
    // A def of a mod's ThingDef subclass is offered where a ThingDef is expected, even though the def index merges
    // before the scopes are ready on load (docs/testing-plan.md step 9).
    [Test] public void TestModDefAsSuperclassReference() => DoNamedTest("ModTypes.cs");
}

/// <summary>
/// Gold is the document after accepting the item named by the input's ${COMPLETE_ITEM:…} directive.
/// Accepting an item commits the edited document, which runs RimworldSymbolScope.Merge mid-commit; these guard that the
/// index doesn't read PSI there (docs/testing-plan.md step 5).
/// </summary>
[TestFileExtension(".xml")]
public class RimworldXmlCompletionActionTests : RimworldCompletionTestBase
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
    protected override string RelativeTestDataPath => @"Completion\Rimworld\Action";

    [Test] public void TestCompleteTag() => DoNamedTest();
    [Test] public void TestCompleteEnumValue() => DoNamedTest();
}
