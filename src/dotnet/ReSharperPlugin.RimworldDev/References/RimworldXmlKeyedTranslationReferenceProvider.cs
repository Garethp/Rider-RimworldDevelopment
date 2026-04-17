using System;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Web.WebConfig;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using ReSharperPlugin.RimworldDev.SymbolScope;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

[ReferenceProviderFactory]
public class RimworldXmlKeyedTranslationReferenceProviderFactory : IReferenceProviderFactory
{
    public RimworldXmlKeyedTranslationReferenceProviderFactory(Lifetime lifetime) =>
        Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);

    public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
    {
        return sourceFile.PrimaryPsiLanguage.Is<XmlLanguage>() && sourceFile.GetExtensionWithDot()
            .Equals(".xml", StringComparison.CurrentCultureIgnoreCase)
            ? new RimworldXmlKeyedTranslationReferenceProvider()
            : null;
    }

    public ISignal<IReferenceProviderFactory> Changed { get; }
}

// This reference provider only serves to attach a reference on the Keyed Translation to itself. It's a hacky workaround
// to allow us to do Find Usages on Keyed Translations
public class RimworldXmlKeyedTranslationReferenceProvider : IReferenceFactory
{
    public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
    {
        if (
            element is not XmlIdentifier identifier ||
            identifier?.Parent?.Parent?.Parent is not XmlTag { } LangaugeDataTag ||
            LangaugeDataTag.Children().First() is not XmlTagHeaderNode languageDataHeaderNode ||
            languageDataHeaderNode.Children().ElementAt(1) is not XmlIdentifier languageDataIdentifier || 
            languageDataIdentifier.GetText() != "LanguageData"
        )
            return new ReferenceCollection();
        
        var keyName = identifier.GetText();
        var xmlSymbolTable = element.GetSolution().GetComponent<RimworldKeyedTranslationSymbolScope>();
        
        var tag = xmlSymbolTable.GetKeyTag(keyName);
        if (tag is null)
            return new ReferenceCollection();

        return new ReferenceCollection(new RimworldKeyedTranslationReference(element, tag, "English", keyName)); 
    }

    public bool HasReference(ITreeNode element, IReferenceNameContainer names)
    {
        if (element.NodeType.ToString() != "TEXT") return false;
        return !element.Parent.GetText().Contains("defName") && names.Contains(element.GetText());
    }
}