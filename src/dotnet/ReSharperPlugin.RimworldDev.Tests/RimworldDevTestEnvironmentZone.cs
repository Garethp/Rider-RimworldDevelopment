using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

[assembly: Apartment(ApartmentState.STA)]

namespace ReSharperPlugin.RimworldDev.Tests;

[ZoneDefinition]
public class RimworldDevTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>;
