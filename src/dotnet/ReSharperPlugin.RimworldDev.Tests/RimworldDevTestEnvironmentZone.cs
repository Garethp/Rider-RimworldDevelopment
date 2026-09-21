using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

#if WINDOWS
// WINDOWS is defined for the net10.0-windows build only. Elsewhere NUnit can't honour STA and would silently run nothing;
// the tests are skipped there by WindowsOnlyGuard instead.
[assembly: Apartment(ApartmentState.STA)]
#endif

namespace ReSharperPlugin.RimworldDev.Tests;

[ZoneDefinition]
public class RimworldDevTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>;
