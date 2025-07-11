using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Application.Threading.Tasks;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.model2.Assemblies.Interfaces;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Threading.Tasks;
using ReSharperPlugin.RimworldDev.Settings;

namespace ReSharperPlugin.RimworldDev;

public class ScopeHelper
{
    private static List<ISymbolScope> allScopes = new();
    private static List<ISymbolScope> knownCustomScopes = new();
    private static ISymbolScope rimworldScope;
    private static IPsiModule rimworldModule;
    private static List<ISymbolScope> usedScopes;
    private static bool adding = false;

    public static bool UpdateScopes(ISolution solution)
    {
        if (solution == null) return false;
        using (CompilationContextCookie.GetOrCreate(UniversalModuleReferenceContext.Instance))
        {
            allScopes = solution.PsiModules().GetModules().Select(module =>
                module.GetPsiServices().Symbols.GetSymbolScope(module, true, true)).ToList();

            // If we haven't determined the Rimworld scope yet, our scopes may not be ready for querying. Since I'd rather
            // that we were able to pull the scope from the dependencies than try to find it ourselves, let's check if the
            // scopes are ready for querying first. Ofcourse, if we have no scopes at all, there's nothing to wait for
            if (rimworldScope == null && allScopes.Any() && allScopes.Any(scope => !scope.GetAllShortNames().Any()))
                return false;

            if (rimworldScope == null)
            {
                rimworldScope =
                    allScopes.FirstOrDefault(scope => scope.GetTypeElementByCLRName("Verse.ThingDef") != null);

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
    }

    public static ISymbolScope GetScopeForClass(string className)
    {
        if (!className.Contains(".")) return rimworldScope;
        if (knownCustomScopes.FirstOrDefault(scope => scope.GetTypeElementByCLRName(className) is not null) is
            { } foundScope) return foundScope;

        if (knownCustomScopes.FirstOrDefault(scope => scope.GetTypeElementByCLRName(className) is not null) is
            { } newCustomScope)
        {
            knownCustomScopes.Add(newCustomScope);
            return newCustomScope;
        }
        
        return rimworldScope;
    }

    private static async void AddRef(ISolution solution)
    {
        if (adding) return;
        adding = true;

        var path = FindRimworldDll(solution.SolutionDirectory.FullPath);
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
        var settings = SettingsAccessor.Instance.GetSettings();
        var customPath = FileSystemPath.TryParse(settings.RimworldPath);
        if (customPath.ExistsFile) customPath = customPath.Parent;
        if (customPath.ExistsDirectory && (customPath.Name.EndsWith(".app") || customPath.GetDirectoryEntries()
                .Any(entry => entry.IsFile && entry.RelativePath.Name is "UnityPlayer.dll" or "UnityPlayer.so")))
        {
            return customPath;
        }

        var locations = new List<FileSystemPath>();

        locations.AddRange(GetSteamLocations()
            .Select(baseDir => FileSystemPath.TryParse($@"{baseDir}/common/RimWorld/")));

        var location = locations.FirstOrDefault(location => location.ExistsDirectory);
        if (location != null) return location;

        // If we're not able to find the Assembly file in the common locations, let's look for it through relative paths
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
                return currentDirectory;
            }
        }

        return null;
    }

    [CanBeNull]
    public static FileSystemPath FindRimworldDll(string currentPath)
    {
        var rimworldLocation = FindRimworldDirectory(currentPath);

        if (rimworldLocation == null) return null;

        var fileRelativePaths = new List<string>
        {
            "RimWorldWin64_Data/Managed/Assembly-CSharp.dll",
            "RimWorldWin_Data/Managed/Assembly-CSharp.dll",
            "RimWorldLinux_Data/Managed/Assembly-CSharp.dll",
            "Contents/Resources/Data/Managed/Assembly-CSharp.dll"
        };

        var location = fileRelativePaths.FirstOrDefault(path =>
            FileSystemPath.ParseRelativelyTo(path, rimworldLocation).ExistsFile);

        if (location == null) return null;

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

        modDirectories.AddRange(GetSteamLocations()
            .Select(location => FileSystemPath.TryParse($"{location}/workshop/content/294100/").FullPath)
            .Where(location => FileSystemPath.TryParse(location).ExistsDirectory)
        );

        return modDirectories;
    }

    private static IEnumerable<string> GetSteamLocations()
    {
        var locations = new List<string>
        {
            @"C:\Program Files (x86)\Steam\steamapps\",
            @"C:\Program Files\Steam\steamapps\",
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/steam/steam/steamapps/",
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/snap/steam/common/.local/share/Steam/steamapps/",
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.local/share/Steam/steamapps/",
            $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.steam/debian-installation/steamapps/common/"
        };

        locations.AddRange(
            DriveInfo
                .GetDrives()
                .Select(drive => $@"{drive.RootDirectory.Name}/SteamLibrary/steamapps/")
                .Select(location => !RuntimeInfo.IsRunningUnderWindows ? $"/{location}" : location)
        );

        return locations
            .Where(location => FileSystemPath.TryParse(location).ExistsDirectory)
            .ToList();
    }

    public static ISymbolScope RimworldScope => rimworldScope;

    public static IPsiModule RimworldModule => rimworldModule;

    public static List<ISymbolScope> AllScopes => allScopes;

    public static Dictionary<string, string> GetModLocations(string basePath, List<string> desiredModIds)
    {
        desiredModIds = desiredModIds.Select(x => x.ToLower()).ToList();
        
        var directoriesToCheck = FindModDirectories(basePath);
        var foundMods = new Dictionary<string, string>();

        foreach (var directory in directoriesToCheck)
        {
            var path = FileSystemPath.TryParse(directory);
            if (!path.ExistsDirectory) continue;

            foreach (var child in path.GetChildren())
            {
                if (!child.IsDirectory) continue;

                var aboutFile = FileSystemPath.TryParse($@"{directory}\{child.ToString()}\About\About.xml");
                if (!aboutFile.ExistsFile) continue;

                var document = GetXmlDocument(aboutFile.FullPath);
                var modId = document?.GetElementsByTagName("ModMetaData")[0]?.GetChildElements("packageId")
                    .FirstOrDefault()?.InnerText;

                if (modId == null || !desiredModIds.Contains(modId?.ToLower())) continue;

                desiredModIds.Remove(modId);
                if (foundMods.ContainsKey(modId)) continue;

                foundMods.Add(modId, aboutFile.FullPath);

                if (desiredModIds.Count == 0) return foundMods;
            }
        }

        return foundMods;
    }


    [CanBeNull]
    public static XmlDocument GetXmlDocument(string fileLocation)
    {
        if (!File.Exists(fileLocation)) return null;

        using var reader = new StreamReader(fileLocation);
        using var xmlReader = new XmlTextReader(reader);
        var document = new XmlDocument();
        document.Load(xmlReader);
        xmlReader.Close();
        reader.Close();

        return document;
    }

    public static List<string> GetAllSuperClasses(string clrName)
    {
        if (clrName.StartsWith("System")) return new List<string>();

        var items = new List<string>();
        var supers = RimworldScope.GetTypeElementByCLRName(clrName)?.GetAllSuperClasses().ToList();
        supers?.ForEach(super =>
        {
            var clr = super.GetClrName().FullName;
            items.Add(clr);
            items.AddRange(GetAllSuperClasses(clr));
        });

        return items;
    }

    public static List<string> GetAllSuperTypeElements(string clrName)
    {
        if (clrName.StartsWith("System")) return new List<string>();

        var items = new List<string>();
        var supers = RimworldScope.GetTypeElementByCLRName(clrName).GetAllSuperTypeElements().ToList();
        supers.ForEach(super =>
        {
            var clr = super.GetClrName().FullName;
            items.Add(clr);
            items.AddRange(GetAllSuperTypeElements(clr));
        });

        return items;
    }

    public static List<string> GetAllSuperTypes(string clrName)
    {
        if (clrName.StartsWith("System")) return new List<string>();

        var items = new List<string>();
        var supers = RimworldScope.GetTypeElementByCLRName(clrName).GetAllSuperTypes().ToList();
        supers.ForEach(super =>
        {
            var clr = super.GetClrName().FullName;
            items.Add(clr);
            items.AddRange(GetAllSuperTypes(clr));
        });

        return items;
    }

    public static bool ExtendsFromVerseDef(string clrName)
    {
        if (RimworldScope is null) return false;

        return
            GetAllSuperClasses(clrName).Any(super => super == "Verse.Def") ||
            GetAllSuperTypes(clrName).Any(super => super == "Verse.Def");
    }
}