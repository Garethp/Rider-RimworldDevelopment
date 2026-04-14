using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Environment;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using JetBrains.TestFramework.Utils;
using JetBrains.Util;
using JetBrains.Util.Logging;
using NUnit.Framework;

// using JetBrains.Application.BuildScript.Application.Zones;
// using JetBrains.ReSharper.Feature.Services;
// using JetBrains.ReSharper.Psi.CSharp;
// using JetBrains.ReSharper.TestFramework;
// using JetBrains.TestFramework;
// using JetBrains.TestFramework.Application.Zones;
// using NUnit.Framework;
//
// namespace ReSharperPlugin.RimworldDev.Tests
// {
//     [ZoneDefinition]
//     public class SamplePluginTestEnvironmentZone : ITestsEnvZone, IRequire<PsiFeatureTestZone> { }
//
//     [ZoneMarker]1
//     public class ZoneMarker : IRequire<ICodeEditingZone>, IRequire<ILanguageCSharpZone>, IRequire<SamplePluginTestEnvironmentZone> { }
//     
//     [SetUpFixture]
//     public class SamplePluginTestsAssembly : ExtensionTestEnvironmentAssembly<SamplePluginTestEnvironmentZone> { }
// }

[assembly: RequiresThread(System.Threading.ApartmentState.STA)]

namespace ReSharperPlugin.RimworldDev.Tests;

// Encapsulates the set of requirements for the host/environment zone (not to be confused with the environment
// container). It is the root product zone that is used to bootstrap and activate the other zones. It is
// automatically activated by ExtensionTestEnvironmentAssembly and is used to mark and therefore include the zone
// activator for the required product zones.
// This should be used only for environment components, as it is one of the only zones active during environment
// container composition.
[ZoneDefinition]
public interface IUnityTestsEnvZone : ITestsEnvZone
{
}

// Encapsulates the set of required product zones needed to run the tests. PsiFeatureTestZone handles most of this,
// adding requirements for zones such as DaemonZone, NavigationZone and ICodeEditingZone, as well as the majority of
// bundled languages. This zone should require or inherit from any custom plugin zones, and explicitly require
// custom languages (PsiFeaturesTestZone does not require IPsiLanguageZone, which would activate all languages via
// inheritance).
// Use this zone for all custom or overriding components in the tests.
[ZoneDefinition]
public interface IUnityTestsZone : IZone,
    IRequire<PsiFeatureTestZone>
{
}

// Activates the product zones required for tests. It is an environment component, and invoked while the environment
// container is being composed. As such, it must be in a zone that is already active - i.e. a host environment zone,
// such as the test env zone. (A completely missing zone marker will also include it in the environment container.)
// It activates the tests zone, which is the set of all zones required to run the tests, including both production
// components and test components, and will be used to filter the shell and solution containers.
[ZoneActivator]
[ZoneMarker(typeof(IUnityTestsEnvZone))]
public class UnityTestZonesActivator : IActivate<IUnityTestsZone>
{
}

[SetUpFixture]
public class TestEnvironment : ExtensionTestEnvironmentAssembly<IUnityTestsEnvZone>
{
    static TestEnvironment()
    {
        try
        {
            // ConfigureLoggingFolderPath();
            // ConfigureStartupLogging();
            // SetJetTestPackagesDir();

            if (PlatformUtil.IsRunningOnMono)
            {
                // Temp workaround for GacCacheController, which adds all Mono GAC paths into a dictionary without
                // checking for duplicates
                Environment.SetEnvironmentVariable("MONO_GAC_PREFIX", "/foo");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}