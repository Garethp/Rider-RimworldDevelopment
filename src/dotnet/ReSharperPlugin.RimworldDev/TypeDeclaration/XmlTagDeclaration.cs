using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Text;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

/**
 * In order to have an IDeclaredElement for XMLTags, we need a IDeclaration implementation for it. For the most part this
 * is just a wrapper around the original ITreeNode
 */
public class XmlTagDeclaration: IDeclaration
{
    private readonly ITreeNode owner;

    public XmlTagDeclaration(ITreeNode owner, IDeclaredElement declaredElement, string declaredName)
    {
        this.owner = owner;
        DeclaredElement = declaredElement;
        DeclaredName = declaredName;
    }

    public IPsiServices GetPsiServices() => owner.GetPsiServices();

    public IPsiModule GetPsiModule() => owner.GetPsiModule();

    public IPsiSourceFile GetSourceFile() => owner.GetSourceFile();

    public ReferenceCollection GetFirstClassReferences() => owner.GetFirstClassReferences();

    public void ProcessDescendantsForResolve(IRecursiveElementProcessor processor) =>
        owner.ProcessDescendantsForResolve(processor);

    public TTreeNode GetContainingNode<TTreeNode>(bool returnThis = false) where TTreeNode : ITreeNode =>
        owner.GetContainingNode<TTreeNode>(returnThis);

    public bool Contains(ITreeNode other) => owner.Contains(other);

    public bool IsPhysical() => owner.IsPhysical();

    public bool IsValid() => owner.IsValid();

    public bool IsFiltered() => owner.IsFiltered();

    public DocumentRange GetNavigationRange() => owner.GetNavigationRange();

    public TreeOffset GetTreeStartOffset() => owner.GetTreeStartOffset();

    public int GetTextLength() => owner.GetTextLength();

    public StringBuilder GetText(StringBuilder to) => owner.GetText(to);

    public IBuffer GetTextAsBuffer() => owner.GetTextAsBuffer();

    public string GetText() => owner.GetText();

    public ITreeNode FindNodeAt(TreeTextRange treeRange) => owner.FindNodeAt(treeRange);

    public IReadOnlyCollection<ITreeNode> FindNodesAt(TreeOffset treeOffset) =>
        owner.FindNodesAt(treeOffset);

    public ITreeNode FindTokenAt(TreeOffset treeTextOffset) => owner.FindTokenAt(treeTextOffset);

    public ITreeNode Parent => owner.Parent;

    public ITreeNode FirstChild => owner.FirstChild;

    public ITreeNode LastChild => owner.LastChild;

    public ITreeNode NextSibling => owner.NextSibling;

    public ITreeNode PrevSibling => owner.PrevSibling;

    public NodeType NodeType => owner.NodeType;

    public PsiLanguageType Language => owner.Language;

    public NodeUserData UserData => owner.UserData;

    public NodeUserData PersistentUserData => owner.PersistentUserData;

    // IDeclaration
    public XmlNode GetXMLDoc(bool inherit) => null;

    public void SetName(string name)
    {
        DeclaredName = name;
    }

    public TreeTextRange GetNameRange() => owner.GetTreeTextRange();

    public bool IsSynthetic() => false;

    public IDeclaredElement DeclaredElement { get; }
    public string DeclaredName { get; set; }
}