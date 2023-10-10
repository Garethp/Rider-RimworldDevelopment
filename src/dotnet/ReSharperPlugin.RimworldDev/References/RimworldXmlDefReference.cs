using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

public class RimworldXmlDefReference :
    TreeReferenceBase<ITreeNode>
{
    private readonly ITreeNode myTypeElement;

    private string myName;
    private string defName;
    private string defType;

    public RimworldXmlDefReference([NotNull] ITreeNode owner, ITreeNode typeElement, string defType, string defName) : base(owner)
    {
        myTypeElement = typeElement;
        myName = defName;
        this.defName = defName;
        this.defType = defType;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        var declaredElement = new XMLTagDeclaredElement(
            myTypeElement,
            defType,
            defName,
            false
        );

        var symbolTable = new SymbolTable(myOwner.GetPsiServices());

        symbolTable.AddSymbol(declaredElement);

        return symbolTable;
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