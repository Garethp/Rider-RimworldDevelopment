using System.Linq;
using System.Xml;
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
using JetBrains.ReSharper.Psi.Xml.Impl.Tree.References;
using JetBrains.ReSharper.Psi.Xml.Tree.References;
using JetBrains.ReSharper.Psi.Xml.Util;
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

/**
 * The reference provider is just that, it provides a reference from XML into C# (And hopefully someday the other way
 * around with Find Usages), where you can Ctrl+Click into the C# for a property or class
 */
public class RimworldReferenceFactory : IReferenceFactory
{
    public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
    {
        if (element.NodeType.ToString() == "TEXT") return GetReferencesForText(element, oldReferences);
        if (element is not XmlIdentifier identifier) return new ReferenceCollection();
        if (element.GetSourceFile() is not { } sourceFile) return new ReferenceCollection();
        
        var solution = sourceFile.PsiModule.GetSolution();

        // TODO: We should really collect all these up into a SymbolScope helper class
        var rimWorldModule = solution.PsiModules().GetModules()
            .First(assembly => assembly.DisplayName == "Assembly-CSharp");

        var rimworldSymbolScope = rimWorldModule.GetPsiServices().Symbols.GetSymbolScope(rimWorldModule, true, true);

        var allSymbolScopes = solution.PsiModules().GetModules().Select(module =>
            module.GetPsiServices().Symbols.GetSymbolScope(module, true, true)).ToList();
            
        var hierarchy = RimworldXMLItemProvider.GetHierarchy(identifier);

        if (hierarchy.Count == 0)
        {
            var @class = rimworldSymbolScope.GetElementsByShortName(identifier.GetText()).FirstOrDefault();
            return new ReferenceCollection(new RimworldXmlReference(@class, identifier));
        }

        var classContext = RimworldXMLItemProvider.GetContextFromHierachy(hierarchy, rimworldSymbolScope, allSymbolScopes);
        if (classContext == null) return new ReferenceCollection();
                
        var field = RimworldXMLItemProvider.GetAllPublicFields(classContext, rimworldSymbolScope)
            .FirstOrDefault(field => field.ShortName == identifier.GetText());

        if (field == null) return new ReferenceCollection();
        
        return new ReferenceCollection(new RimworldXmlReference(field, identifier));
    }

    private ReferenceCollection GetReferencesForText(ITreeNode element, ReferenceCollection oldReferences)
    {
        // Trying to figure out how to add references to other XML. Still no clue
        if (element.GetText() == "EatAtCannibalFuneral" && false)
        {
            var tagId = $"DutyDef/{element.GetText()}";
            if (!RimworldXMLDefUtil.DefTags.ContainsKey(tagId)) return new ReferenceCollection();
            
            var tag = RimworldXMLDefUtil.DefTags["DutyDef/EatAtCannibalFuneral"];

            var symbolScope = tag.GetResolveContext().GetCaseSensitiveSymbolScopeWithReferences();
            var test = symbolScope.GetAllTypeElementsGroupedByName().Where(typeElement =>
                typeElement.HasMemberWithName("EatAtCannibalFuneral", false)).ToList();
            
            var allNames = symbolScope.GetAllTypeMemberNames();
            
            return tag.GetFirstClassReferences();
            // return new ReferenceCollection(new RimworldXmlDefReference(tag, element));

            var a = 1 + 1;
        }
        
        return new ReferenceCollection();
    }
    
    public bool HasReference(ITreeNode element, IReferenceNameContainer names)
    {
        return true;
    }
}