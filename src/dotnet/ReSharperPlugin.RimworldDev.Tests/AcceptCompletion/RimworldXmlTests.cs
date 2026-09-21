using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.CompletionSuggestions;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.AcceptCompletion;

[ProjectLayouts(ProjectLayout.XmlProject, ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldXmlTests(ProjectLayout layout) : RimworldCompletionTestBase(layout)
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;
    protected override string RelativeTestDataPath => @"AcceptCompletion\Rimworld";

    [Test] public void TestCompleteTag() => DoNamedTest();
    [Test] public void TestCompleteEnumValue() => DoNamedTest();
}