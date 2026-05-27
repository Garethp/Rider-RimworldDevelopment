using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.RimworldDev.SymbolScope;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

public class RimworldKeyedTranslationReference : TreeReferenceBase<ITreeNode>
{
    private readonly ITreeNode myTypeElement;

    private string myName;
    private string keyName;
    private string language;

    public RimworldKeyedTranslationReference([NotNull] ITreeNode owner, ITreeNode typeElement, string laguage, string keyName) : base(owner)
    {
        myTypeElement = typeElement;
        myName = $"{laguage}/{keyName}";
        this.keyName = keyName;
        this.language = laguage;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        var symbolScope = myOwner.GetSolution().GetComponent<RimworldKeyedTranslationSymbolScope>();

        symbolScope.AddDeclaredElement(
            myOwner.GetSolution(),
            myTypeElement,
            language,
            keyName,
            false
        );
        
        return symbolScope.GetSymbolTable(myOwner.GetSolution());
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
        return GetReferenceSymbolTable(true).GetResolveResult(GetName());
    }

    public override string GetName() => myName;

    public override IReference BindTo(IDeclaredElement element) =>
        BindTo(element, EmptySubstitution.INSTANCE);

    public override IReference BindTo(
        IDeclaredElement element,
        ISubstitution substitution)
    {
        return this;
    }

    public override TreeTextRange GetTreeTextRange() => myOwner.GetTreeTextRange();

    public override IAccessContext GetAccessContext() => new ElementAccessContext(myOwner);
}