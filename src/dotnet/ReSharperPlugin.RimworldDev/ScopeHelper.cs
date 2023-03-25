using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;

namespace ReSharperPlugin.RimworldDev;

public class ScopeHelper
{
    private static List<ISymbolScope> allScopes = new();
    private static ISymbolScope rimworldScope;
    private static IPsiModule rimworldModule;
    private static List<ISymbolScope> usedScopes;

    public static bool UpdateScopes(ISolution solution)
    {
        if (solution == null) return false;
        
        allScopes = solution.PsiModules().GetModules().Select(module =>
            module.GetPsiServices().Symbols.GetSymbolScope(module, true, true)).ToList();

        if (rimworldScope == null)
        {
            rimworldScope = allScopes.FirstOrDefault(scope => scope.GetTypeElementByCLRName("Verse.ThingDef") != null);

            if (rimworldScope == null) return false;
            
            rimworldModule = solution.PsiModules().GetModules()
                .First(module => module.GetPsiServices().Symbols.GetSymbolScope(module, true, true).GetTypeElementByCLRName("Verse.ThingDef") != null);

            // rimworldScope = rimWorldModule.GetPsiServices().Symbols.GetSymbolScope(rimWorldModule, true, true);
        }

        return true;
    }

    public static ISymbolScope RimworldScope => rimworldScope;

    public static IPsiModule RimworldModule => rimworldModule;

    public static List<ISymbolScope> AllScopes => allScopes;
}