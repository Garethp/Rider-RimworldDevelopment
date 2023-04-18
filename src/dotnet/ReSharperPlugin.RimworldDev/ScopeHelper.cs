using System.Collections.Generic;
using System.Linq;
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

        var locations = new List<string>
        {
            "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll",
            "C:\\Program Files\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll",
            "~/.steam/steam/SteamApps/common/RimWorld/RimWorldWin64_Data/Managed/Assembly-CSharp.dll"
        };


        var location = locations.FirstOrDefault(location => FileSystemPath.TryParse(location).ExistsFile);

        if (location == null) return;
        
        var path = FileSystemPath.TryParse(location);

        var moduleReferenceResolveContext =
            (IModuleReferenceResolveContext)UniversalModuleReferenceContext.Instance;

        await solution.Locks.Tasks.YieldTo(solution.GetLifetime(), Scheduling.MainDispatcher, TaskPriority.Low);

        solution.GetComponent<IAssemblyFactory>().AddRef(path.ToAssemblyLocation(), "ScopeHelper::AddRef",
            moduleReferenceResolveContext);
    }

    public static ISymbolScope RimworldScope => rimworldScope;

    public static IPsiModule RimworldModule => rimworldModule;

    public static List<ISymbolScope> AllScopes => allScopes;
}