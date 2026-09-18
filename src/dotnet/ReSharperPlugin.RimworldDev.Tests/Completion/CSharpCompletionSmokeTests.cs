using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using NUnit.Framework;

namespace ReSharperPlugin.RimworldDev.Tests.Completion;

/// <summary>
/// Proves that CodeCompletionTestBase + gold files work in this harness at all, with nothing RimWorld-specific
/// involved. If this is red, the completion pipeline itself is broken, not our provider.
/// </summary>
public class CSharpCompletionSmokeTests : CodeCompletionTestBase
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
    protected override string RelativeTestDataPath => @"Completion\CSharp";

    [Test] public void TestLocalVariable() => DoNamedTest();
}
