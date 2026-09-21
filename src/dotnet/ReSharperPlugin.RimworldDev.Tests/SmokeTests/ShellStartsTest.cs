using JetBrains.TestFramework;
using NUnit.Framework;

namespace ReSharperPlugin.RimworldDev.Tests.SmokeTests;

/// <summary>
/// The smoke tests are here to prove that tests, in general, are functioning. This is in case we do an upgrade and find
/// our automated tests are no longer functioning. It allows us to narrow it down to whether tests are broken, parsing
/// is broken, completion is broken or just our specific tests are broken.
///
/// This test is just checking that we can start tests in the first place.
/// </summary>
public class ShellStartsTest : BaseTest
{
    [Test]
    public void ShellStarts()
    {
        Assert.That(ShellInstance, Is.Not.Null);
    }
}
