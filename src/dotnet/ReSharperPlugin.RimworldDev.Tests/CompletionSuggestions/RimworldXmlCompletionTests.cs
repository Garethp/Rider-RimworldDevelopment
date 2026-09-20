using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.CompletionSuggestions;

/// <summary>
/// The real thing: RimWorld XML completion backed by the game's types. Gold is the lookup list at {caret}.
/// </summary>
[ProjectLayouts(ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldXmlCompletionTests(ProjectLayout layout) : RimworldCompletionTestBase(layout)
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
    protected override string RelativeTestDataPath => @"CompletionSuggestions\Rimworld";

    // Test Property Names
    [Test] public void TestDefName() => DoNamedTest();
    [Test] public void TestThingDefProperties() => DoNamedTest();
    [Test] public void TestModDefClassProperties() => DoNamedTest("ModTypes.cs");
    [Test] public void TestNestedFieldProperties() => DoNamedTest();
    [Test] public void TestListItemProperties() => DoNamedTest();
    [Test] public void TestListItemWithClassProperties() => DoNamedTest();
    [Test] public void TestModListItemClassProperties() => DoNamedTest("ModTypes.cs");
    
    // Test Property Values
    [Test] public void TestBooleanPropertyValue() => DoNamedTest();
    [Test] public void TestEnumValue() => DoNamedTest();

    [Test] public void TestStructPropertyValue() => DoNamedTest();
    
    // Test Def references
    [Test] public void TestDefReferenceSameFile() => DoNamedTest();
    [Test] public void TestDefReferenceOtherFile() => DoNamedTest("OtherDefs.xml");

    [Test]
    public void TestDefsFilterByType() => DoNamedTest("OtherDefs.xml", "StuffCategoryDefs.xml");
    [Test] public void TestParentName() => DoNamedTest();
    
    // When a property expects a specific DefType (like ThingDef), modded classes that extend that should be offered
    [Test] public void TestModDefAsSuperclassReference() => DoNamedTest("ModTypes.cs");
}