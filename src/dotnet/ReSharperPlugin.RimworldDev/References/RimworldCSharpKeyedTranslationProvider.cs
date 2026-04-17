using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.RimworldDev.SymbolScope;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

[ReferenceProviderFactory]
public class RimworldCSharpKeyedTranslationProvider : IReferenceProviderFactory
{
    public RimworldCSharpKeyedTranslationProvider(Lifetime lifetime) =>
        Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);
    
    public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks)
    {
        return sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>()
            ? new RimworldCSharpKeyedTranslationReferenceFactory()
            : null;
    }

    public ISignal<IReferenceProviderFactory> Changed { get; }
}

public class RimworldCSharpKeyedTranslationReferenceFactory : IReferenceFactory
{
    public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
    {
        if (element is not ICSharpLiteralExpression ||
            element.NextSibling?.NextSibling is not ICSharpIdentifier identifier ||
            identifier.GetText() != "Translate")
            return new ReferenceCollection();

        var key = element.GetUnquotedText();
        var xmlSymbolTable = element.GetSolution().GetComponent<RimworldKeyedTranslationSymbolScope>();

        var tag = xmlSymbolTable.GetKeyTag(key);
        if (tag is null)
            return new ReferenceCollection();
        
        return new ReferenceCollection(new RimworldKeyedTranslationReference(element, tag, "English", key)); 
    }

    public bool HasReference(ITreeNode element, IReferenceNameContainer names)
    {
        if (element.NodeType.ToString() != "TEXT") return false;
        return !element.Parent.GetText().Contains("defName") && names.Contains(element.GetText());
    }
}