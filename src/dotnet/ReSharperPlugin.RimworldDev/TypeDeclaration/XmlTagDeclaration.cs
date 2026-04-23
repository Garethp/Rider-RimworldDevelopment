using System.Collections.Generic;
using System.Text;
using System.Xml;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

/**
 * In order to have an IDeclaredElement for XMLTags, we need a IDeclaration implementation for it. For the most part this
 * is just a wrapper around the original ITreeNode
 */
public class XmlTagDeclaration: CompositeElement, IDeclaration
{
    private readonly ITreeNode Owner;

    public XmlTagDeclaration(ITreeNode owner, IDeclaredElement declaredElement, string declaredName)
    {
        var ownerElement = (TreeElement)owner;
        parent = ownerElement.parent;
        Owner = owner;
        DeclaredElement = declaredElement;
        DeclaredName = declaredName;
    }

    public IDeclaredElement DeclaredElement { get; }
    public string DeclaredName { get; set; }

    public override NodeType NodeType => Owner.NodeType;

    public override PsiLanguageType Language => Owner.Language;

    public XmlNode GetXMLDoc(bool inherit) => null;

    public void SetName(string name)
    {
        DeclaredName = name;
    }

    public TreeTextRange GetNameRange()
    {
        return Owner.GetTreeTextRange();
    }

    public bool IsSynthetic() => false;

    public override DocumentRange GetNavigationRange()
    {
        return Owner.GetDocumentRange();
    }

    public override bool IsValid()
    {
        return Owner.IsValid() && DeclaredElement is not null && DeclaredElement.IsValid();
    }
}