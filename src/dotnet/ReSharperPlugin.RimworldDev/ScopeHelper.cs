using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.model2.Assemblies.Interfaces;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using JetBrains.Util.Threading.Tasks;

namespace ReSharperPlugin.RimworldDev;

public class ScopeHelper
{
    private static List<ISymbolScope> allScopes = new();
    private static ISymbolScope rimworldScope;
    private static IPsiModule rimworldModule;
    private static List<ISymbolScope> usedScopes;
    private static bool adding = false;

    public static bool UpdateScopes(ISolution solution)
    {
        if (solution == null) return false;

        allScopes = solution.PsiModules().GetModules().Select(module =>
            module.GetPsiServices().Symbols.GetSymbolScope(module, true, true)).ToList();

        // If we haven't determined the Rimworld scope yet, our scopes may not be ready for querying. Since I'd rather
        // that we were able to pull the scope from the dependencies than try to find it ourselves, let's check if the
        // scopes are ready for querying first. Ofcourse, if we have no scopes at all, there's nothing to wait for
        if (rimworldScope == null && allScopes.Any() && allScopes.Any(scope => !scope.GetAllShortNames().Any()))
            return false;

        if (rimworldScope == null)
        {
            rimworldScope = allScopes.FirstOrDefault(scope => scope.GetTypeElementByCLRName("Verse.ThingDef") != null);

            if (rimworldScope == null)
            {
                AddRef(solution);

                return false;
            }

            rimworldModule = solution.PsiModules().GetModules()
                .First(module =>
                    module.GetPsiServices().Symbols.GetSymbolScope(module, true, true)
                        .GetTypeElementByCLRName("Verse.ThingDef") != null);
        }

        return true;
    }

    private static async void AddRef(ISolution solution)
    {
        if (adding) return;
        adding = true;

        var path = FindRimworldDLL(solution.SolutionDirectory.FullPath);
        if (path == null) return;

        var moduleReferenceResolveContext =
            (IModuleReferenceResolveContext)UniversalModuleReferenceContext.Instance;

        await solution.Locks.Tasks.YieldTo(solution.GetSolutionLifetimes().MaximumLifetime, Scheduling.MainDispatcher,
            TaskPriority.Low);

        solution.GetComponent<IAssemblyFactory>().AddRef(path.ToAssemblyLocation(), "ScopeHelper::AddRef",
            moduleReferenceResolveContext);
    }

    [CanBeNull]
    public static FileSystemPath FindRimworldDirectory(string currentPath)
    {
        var locations = new List<string>
        {
            "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll",
            "C:\\Program Files\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll",
            "~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed/Assembly-CSharp.dll"
        };

        var location = locations.FirstOrDefault(location => FileSystemPath.TryParse(location).ExistsFile);

        // If we're not able to find the Assembly file in the common locations, let's look for it through relative paths
        if (location == null)
        {
            var fileRelativePaths = new List<string>
            {
                "RimWorldWin64_Data/Managed/Assembly-CSharp.dll",
                "RimWorldWin_Data/Managed/Assembly-CSharp.dll",
                "RimWorldLinux_Data/Managed/Assembly-CSharp.dll",
                "Contents/Resources/Data/Managed/Assembly-CSharp.dll"
            };

            var currentDirectory = FileSystemPath.TryParse(currentPath);

            // we're going to look up parent directories 5 times
            for (var i = 0; i < 5; i++)
            {
                currentDirectory = currentDirectory.Parent;
                if (currentDirectory.Exists == FileSystemPath.Existence.Missing) break;

                // If we spot UnityPlayer.dll, we're in the correct directory, we'll either find our Assembly-CSharp.dll
                // relative to here or not at all
                if (currentDirectory.Name.EndsWith(".app") || currentDirectory.GetDirectoryEntries()
                        .Any(entry => entry.IsFile && entry.RelativePath.Name is "UnityPlayer.dll" or "UnityPlayer.so"))
                {
                    // We've got a few different possible relative locations for Assembly-CSharp.dll, let's check there
                    location = currentDirectory.FullPath;

                    break;
                }
            }

            if (location == null) return null;
        }

        var path = FileSystemPath.TryParse(location);
        return !path.ExistsDirectory ? null : path;
    }

    [CanBeNull]
    public static FileSystemPath FindRimworldDLL(string currentPath)
    {
        var rimworldLocation = FindRimworldDirectory(currentPath);
        
        var fileRelativePaths = new List<string>
        {
            "RimWorldWin64_Data/Managed/Assembly-CSharp.dll",
            "RimWorldWin_Data/Managed/Assembly-CSharp.dll",
            "RimWorldLinux_Data/Managed/Assembly-CSharp.dll",
            "Contents/Resources/Data/Managed/Assembly-CSharp.dll"
        };

        var location = fileRelativePaths.FirstOrDefault(path =>
            FileSystemPath.ParseRelativelyTo(path, rimworldLocation).ExistsFile);

        var path = FileSystemPath.ParseRelativelyTo(location, rimworldLocation);

        return path.ExistsFile ? path : null;
    }

    public static List<string> FindModDirectories(string currentPath)
    {
        var modDirectories = new List<string>();
        var rimworldDirectory = FindRimworldDirectory(currentPath);

        if (rimworldDirectory != null)
        {
            var dataDirectory = FileSystemPath.TryParse($@"{rimworldDirectory.FullPath}/Data");
            var modsDirectory = FileSystemPath.TryParse($@"{rimworldDirectory.FullPath}/Mods");

            if (dataDirectory.ExistsDirectory) modDirectories.Add(dataDirectory.FullPath);
            if (modsDirectory.ExistsDirectory) modDirectories.Add(modsDirectory.FullPath);
        }

        return modDirectories;
    }

    public static ISymbolScope RimworldScope => rimworldScope;

    public static IPsiModule RimworldModule => rimworldModule;

    public static List<ISymbolScope> AllScopes => allScopes;
}