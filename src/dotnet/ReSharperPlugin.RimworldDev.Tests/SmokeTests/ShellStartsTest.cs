using JetBrains.TestFramework;
using NUnit.Framework;

namespace ReSharperPlugin.RimworldDev.Tests.SmokeTests;

/// <summary>
/// Proves the ReSharper test shell boots at all. If this is red, nothing else in the project can be green.
/// </summary>
public class ShellStartsTest : BaseTest
{
    [Test]
    public void ShellStarts()
    {
        Assert.That(ShellInstance, Is.Not.Null);
    }
}
