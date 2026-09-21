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
    [Test] public void TestDefOfFieldWithPrefix() => DoNamedTest("Defs.xml");
    [Test] public void TestDefDatabaseGetNamed() => DoNamedTest("Defs.xml");
}
