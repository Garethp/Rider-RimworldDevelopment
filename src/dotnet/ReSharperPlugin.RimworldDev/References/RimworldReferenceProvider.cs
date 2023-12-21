using System;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Web.WebConfig;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using ReSharperPlugin.RimworldDev.SymbolScope;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

[ReferenceProviderFactory]
public class RimworldReferenceProvider : IReferenceProviderFactory
{
    public RimworldReferenceProvider(Lifetime lifetime) =>
        Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);

    public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
    {
        return sourceFile.PrimaryPsiLanguage.Is<XmlLanguage>() && sourceFile.GetExtensionWithDot()
            .Equals(".xml", StringComparison.CurrentCultureIgnoreCase)
            ? new RimworldReferenceFactory()
            : null;
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
        if (!ScopeHelper.UpdateScopes(element.GetSolution())) return new ReferenceCollection();

        if (element.Parent != null && element.NodeType.ToString() == "TEXT" &&
            element.Parent.GetText().StartsWith("<defName>"))
            return GetReferenceForDeclaredElement(element, oldReferences);
        
        if (element.NodeType.ToString() == "TEXT") return GetReferencesForText(element, oldReferences);
        if (element is not XmlIdentifier identifier) return new ReferenceCollection();
        if (element.GetSourceFile() is not { } sourceFile) return new ReferenceCollection();

        var rimworldSymbolScope = ScopeHelper.RimworldScope;
        var allSymbolScopes = ScopeHelper.AllScopes;

        var hierarchy = RimworldXMLItemProvider.GetHierarchy(identifier);

        if (hierarchy.Count == 0)
        {
            var className = identifier.GetText();

            var allWorkingScopes =
                ScopeHelper.AllScopes.Where(scope => scope.GetTypeElementByCLRName(className) is not null);
            
            var @class = className.Contains(".")
                ? ScopeHelper.GetScopeForClass(className).GetTypeElementByCLRName(className)
                : rimworldSymbolScope.GetElementsByShortName(className).FirstOrDefault();
            
            if (@class == null) return new ReferenceCollection();
            return new ReferenceCollection(new RimworldXmlReference(@class, identifier));
        }

        var classContext =
            RimworldXMLItemProvider.GetContextFromHierachy(hierarchy, rimworldSymbolScope, allSymbolScopes);
        if (classContext == null) return new ReferenceCollection();

        var field = RimworldXMLItemProvider.GetAllFields(classContext, rimworldSymbolScope)
            .FirstOrDefault(field => field.ShortName == identifier.GetText());

        if (field == null) return new ReferenceCollection();

        return new ReferenceCollection(new RimworldXmlReference(field, identifier));
    }

    private ReferenceCollection GetReferencesForText(ITreeNode element, ReferenceCollection oldReferences)
    {
        var hierarchy = RimworldXMLItemProvider.GetHierarchy(element);

        if (hierarchy.Count == 0) return new ReferenceCollection();

        var classContext =
            RimworldXMLItemProvider.GetContextFromHierachy(hierarchy, ScopeHelper.RimworldScope, ScopeHelper.AllScopes);
        if (classContext == null) return new ReferenceCollection();

        var rimworldSymbolScope = ScopeHelper.RimworldScope;
        var allSymbolScopes = ScopeHelper.AllScopes;

        if (classContext.GetType().Name == "Enum")
        {
            var @class = rimworldSymbolScope.GetElementsByShortName(classContext.ShortName).FirstOrDefault();
            var col = new ReferenceCollection(new RimworldXmlReference(@class, element));

            return col;
            // return new ReferenceCollection();
        }

        if (!ScopeHelper.ExtendsFromVerseDef(classContext.GetClrName().FullName))
            return new ReferenceCollection();

        var xmlSymbolTable = element.GetSolution().GetComponent<RimworldSymbolScope>();

        var defType = classContext.ShortName;
        var defName = element.GetText();
        
        if (xmlSymbolTable.ExtraDefTagNames.ContainsKey($"{defType}/{defName}"))
        {
            var newDef = xmlSymbolTable.ExtraDefTagNames[$"{defType}/{defName}"];
            defType = newDef.Split('/').First();
            defName = newDef.Split('/').Last();
        }
        
        if (xmlSymbolTable.GetTagByDef(defType, defName) is not { } tag)
            return new ReferenceCollection();
        
        return new ReferenceCollection(new RimworldXmlDefReference(element, tag, defType, defName));
    }

    /**
     * This is a bit of a hacky workaround. Since we're not constructing our own Custom Language, we don't have control
     * over the Psi Tree. Unfortunately, Find Usages expects to be able to find an `IDeclaration` which is a `ITreeNode`
     * in the Psi Tree, which would be created on the Tree Construction.
     *
     * Since we can't do that, in order to be able to invoke Find Usages on the declared element itself, we're going to
     * make a hacky workaround and just give it a reference... to itself
     */
    private ReferenceCollection GetReferenceForDeclaredElement(ITreeNode element, ReferenceCollection oldReferences)
    {
        // We're currently in a text node inside a <defName> inside another ThingDef node. We want to get that node
        var defTypeName = element.Parent?.Parent?
            // And then get the TagHeader (<ThingDef>) of that node
            .Children().FirstOrDefault(childElement => childElement is XmlTagHeaderNode)?
            // And then get the text that provides the ID of that node (ThingDef)
            .Children().FirstOrDefault(childElement => childElement is XmlIdentifier)?.GetText();

        if (defTypeName is null) new ReferenceCollection();

        var defName = element.GetText();

        var xmlSymbolTable = element.GetSolution().GetComponent<RimworldSymbolScope>();

        var tagId = $"{defTypeName}/{defName}";
        if (!xmlSymbolTable.DefTags.ContainsKey(tagId)) return new ReferenceCollection();

        if (xmlSymbolTable.GetTagByDef(defTypeName, defName) is not { } tag)
            return new ReferenceCollection();

        return new ReferenceCollection(new RimworldXmlDefReference(element, tag, defTypeName,
            element.GetText()));
    }

    public bool HasReference(ITreeNode element, IReferenceNameContainer names)
    {
        if (element.NodeType.ToString() != "TEXT") return false;
        return !element.Parent.GetText().Contains("defName") && names.Contains(element.GetText());
    }
}