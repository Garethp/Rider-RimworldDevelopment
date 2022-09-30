using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree.References;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

public class RimworldXmlDefReference :
    TreeReferenceBase<ITreeNode>
{
    private readonly IXmlTag myTypeElement;

    private readonly string myName;

    public RimworldXmlDefReference([NotNull] ITreeNode owner, IXmlTag typeElement, string name) : base(owner)
    {
        myTypeElement = typeElement;
        myName = name;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        var symbolTable = new SymbolTable(myOwner.GetPsiServices());

        symbolTable.AddSymbol(
            new XMLTagDeclaredElement(
                myTypeElement,
                myName,
                false
            )
        );

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
