using JetBrains.TestFramework;
using NUnit.Framework;

namespace ReSharperPlugin.RimworldDev.Tests;

/// <summary>
/// Proves the ReSharper test shell boots at all. If this is red, nothing else in the project can be green.
/// </summary>
public class SmokeTests : BaseTest
{
    [Test]
    public void ShellStarts()
    {
        Assert.That(ShellInstance, Is.Not.Null);
    }
}
