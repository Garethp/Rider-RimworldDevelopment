using System;
using NUnit.Framework;

/// <summary>
/// Everything in this assembly boots the ReSharper test shell, which needs the Windows Desktop runtime (WPF/WinForms)
/// and can't run elsewhere (docs/testing-research.md, "Platform"). Off Windows this reports every test as skipped, with
/// the reason, instead of letting the shell crash on startup.
/// </summary>
/// <remarks>
/// Deliberately outside any namespace: NUnit runs set-up fixtures from the outermost namespace in, so this runs before
/// <c>RimworldDevTestsAssembly</c> (which boots the shell), and <c>Assert.Ignore</c> here marks each test beneath it as
/// ignored. An assembly-level <c>[Platform]</c> doesn't work for this: the adapter then reports "No test is available"
/// and nothing else, which reads as a pass. OS-agnostic tests would need their own assembly.
/// </remarks>
[SetUpFixture]
public class WindowsOnlyGuard
{
    [OneTimeSetUp]
    public void RequireWindows()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Ignore("The ReSharper test shell only runs on Windows; run the suite on Windows (CI: add the 'feature-testing' PR label).");
    }
}
