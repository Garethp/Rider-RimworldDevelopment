using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Threading;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Assemblies.Impl;
using JetBrains.ProjectModel.model2.Assemblies.Interfaces;
using JetBrains.ProjectModel.Model2.Assemblies.Interfaces;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;

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
                if (!adding)
                {
                    var location =
                        "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll";
                    var path = FileSystemPath.TryParse(location);

                    var moduleReferenceResolveContext =
                        (IModuleReferenceResolveContext)UniversalModuleReferenceContext.Instance;

                    IShellLocks shellLocks = solution.GetComponent<IShellLocks>();

                    // using (shellLocks.UsingWriteLock("Src\\TestFramework\\BaseTestWithSolution.cs"))
                        solution.GetComponent<IAssemblyFactory>().AddRef(path.ToAssemblyLocation(),
                            "SolutionTestExtensions::AddAssembly", moduleReferenceResolveContext);

                    adding = true;
                }

                return false;
            };
            
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