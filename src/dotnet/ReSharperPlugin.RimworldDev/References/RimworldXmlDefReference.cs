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
using ReSharperPlugin.RimworldDev.SymbolScope;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

public class RimworldXmlDefReference :
    TreeReferenceBase<ITreeNode>
{
    private readonly IXmlTag myTypeElement;

    private string myName;

    public RimworldXmlDefReference([NotNull] ITreeNode owner, IXmlTag typeElement, string name) : base(owner)
    {
        myTypeElement = typeElement;
        myName = name;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        var symbolScope = myOwner.GetSolution().GetComponent<RimworldSymbolScope>();

        symbolScope.AddDeclaredElement(
            myOwner.GetSolution(),
            myTypeElement,
            myName,
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