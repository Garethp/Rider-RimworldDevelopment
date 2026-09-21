using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using NUnit.Framework;

namespace ReSharperPlugin.RimworldDev.Tests.SmokeTests;

/// <summary>
/// The smoke tests are here to prove that tests, in general, are functioning. This is in case we do an upgrade and find
/// our automated tests are no longer functioning. It allows us to narrow it down to whether tests are broken, parsing
/// is broken, completion is broken or just our specific tests are broken.
///
/// This test is specific to completion and just tests if we can assert on autocompleting a C# variable.
/// </summary>
public class CSharpCompletion : CodeCompletionTestBase
{
    protected override CodeCompletionTestType TestType => CodeCompletionTestType.ModernList;
    protected override string RelativeTestDataPath => @"SmokeTests\CSharp";

    [Test] public void TestLocalVariable() => DoNamedTest();
}
