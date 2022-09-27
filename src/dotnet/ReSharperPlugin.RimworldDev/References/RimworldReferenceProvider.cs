using System.Linq;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.UnitTesting.Analysis.Xunit.References;

namespace ReSharperPlugin.RimworldDev.References;

[ReferenceProviderFactory]
public class RimworldReferenceProvider : IReferenceProviderFactory
{
    public RimworldReferenceProvider(Lifetime lifetime) => 
        Changed =new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);

    public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
    {
        return sourceFile.PrimaryPsiLanguage.Is<XmlLanguage>() ? new RimworldReferenceFactory() : null;
    }

    public ISignal<IReferenceProviderFactory> Changed { get; }
}

public class RimworldReferenceFactory : IReferenceFactory
{
    public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
    {
        // if (element is XmlFloatingTextToken && element.GetText() == "CannibalFuneral" && element.Parent is XmlTag parentTag && RimworldXMLItemProvider.GetTagName(parentTag) == "issue")
        // {
            // var a = 1 + 1;
        // }
        
        if (element is XmlIdentifier identifier)
        {
            var solution = element.GetSourceFile().PsiModule.GetSolution();

            var rimWorldModule = solution.PsiModules().GetModules()
                .First(assembly => assembly.DisplayName == "Assembly-CSharp");

            var rimworldSymbolScope = rimWorldModule.GetPsiServices().Symbols.GetSymbolScope(rimWorldModule, true, true);

            var allSymbolScopes = solution.PsiModules().GetModules().Select(module =>
                module.GetPsiServices().Symbols.GetSymbolScope(module, true, true)).ToList();
            
            var hierarchy = RimworldXMLItemProvider.GetHierarchy(element);

            if (hierarchy.Count == 0)
            {
                var @class = rimworldSymbolScope.GetElementsByShortName(identifier.GetText()).FirstOrDefault();
                return new ReferenceCollection(new RimworldXmlReference(@class, element as XmlIdentifier));
            }
            else
            {
                var classContext = RimworldXMLItemProvider.GetContextFromHierachy(hierarchy, rimworldSymbolScope, allSymbolScopes);
                if (classContext == null) return new ReferenceCollection();
                
                var field = RimworldXMLItemProvider.GetAllPublicFields(classContext, rimworldSymbolScope)
                    .FirstOrDefault(field => field.ShortName == identifier.GetText());

                if (field != null)
                {
                    return new ReferenceCollection(new RimworldXmlReference(field, element as XmlIdentifier));
                }
            }
        }
        return new ReferenceCollection();
    }

    public bool HasReference(ITreeNode element, IReferenceNameContainer names)
    {
        return false;
    }
}