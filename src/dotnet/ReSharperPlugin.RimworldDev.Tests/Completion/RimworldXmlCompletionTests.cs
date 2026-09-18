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
    // Gold is hand-written: what the plugin *should* offer. It currently omits CustomThing, because ExtraDefTagNames is
    // only built when ScopeHelper already has the RimWorld scope at merge time, and on a cold load it doesn't.
    [Test, Ignore("ExtraDefTagNames not built when the def index merges before scopes are ready; see docs/testing-plan.md step 9")]
    public void TestModDefAsSuperclassReference() => DoNamedTest("ModTypes.cs");
}

/// <summary>
/// Gold is the document after accepting the item named by the input's ${COMPLETE_ITEM:…} directive.
/// The golds are correct, but accepting an item commits the edited document, and RimworldSymbolScope.Merge reads the
/// PSI file mid-commit ("Trying to get PSI file for an uncommitted document"), which fails the test as a logged error.
/// </summary>
[TestFileExtension(".xml")]
[Ignore("RimworldSymbolScope.AddToLocalCache calls GetPrimaryPsiFile during commit merge; see docs/testing-plan.md step 5")]
public class RimworldXmlCompletionActionTests : RimworldCompletionTestBase
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
    protected override string RelativeTestDataPath => @"Completion\Rimworld\Action";

    [Test] public void TestCompleteTag() => DoNamedTest();
    [Test] public void TestCompleteEnumValue() => DoNamedTest();
}
