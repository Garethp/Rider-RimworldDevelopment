using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.CompletionSuggestions;

/// <summary>
/// Def names offered in C#, from the same def index the XML side uses. Each test brings Defs.xml along so the index has
/// something in it.
/// </summary>
[TestFileExtension(".cs")]
[ProjectLayouts(ProjectLayout.CSharpProject)]
public class RimworldCSharpCompletionTests(ProjectLayout layout) : RimworldCompletionTestBase(layout)
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
    protected override string RelativeTestDataPath => @"CompletionSuggestions\RimworldCSharp";

    [Test] public void TestDefOfFieldWithPrefixAndSemicolon() => DoNamedTest("Defs.xml");

    // Gold is hand-written: what the plugin *should* offer. Without a following ';' the C# parser recovers the unfinished
    // `public static ThingDef Mod` as a MethodDeclaration, and CSharpDefsOfItemProvider.IsAvailable wants a
    // FieldDeclaration, so only C#'s own name suggestions appear.
    [Test, Ignore("CSharpDefsOfItemProvider needs a FieldDeclaration; unfinished declarations parse as methods; see docs/testing-plan.md step 10")]
    public void TestDefOfFieldWithPrefix() => DoNamedTest("Defs.xml");
    
    [Test] public void TestDefDatabaseGetNamed() => DoNamedTest("Defs.xml");
}
