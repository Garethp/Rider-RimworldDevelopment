using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.CompletionSuggestions;

[TestFileExtension(".cs")]
[ProjectLayouts(ProjectLayout.CSharpProject)]
public class RimworldCSharpCompletionTests(ProjectLayout layout) : RimworldCompletionTestBase(layout)
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
    protected override string RelativeTestDataPath => @"CompletionSuggestions\RimworldCSharp";

    [Test] public void TestDefOfFieldWithPrefixAndSemicolon() => DoNamedTest("Defs.xml");

    /// <summary>
    /// Because of how we've set up the ItemLookup, if we do `public static ThingDef Mod{caret};`, it'll suggest our
    /// correct ThingDefs, however if we leave the semi-colon off and do `public static ThingDef Mod{caret}` then it
    /// won't.
    /// </summary>
    [Test, Ignore("CSharpDefsOfItemProvider needs to be adjusted first")]
    public void TestDefOfFieldWithPrefix() => DoNamedTest("Defs.xml");
    
    [Test] public void TestDefDatabaseGetNamed() => DoNamedTest("Defs.xml");
}
