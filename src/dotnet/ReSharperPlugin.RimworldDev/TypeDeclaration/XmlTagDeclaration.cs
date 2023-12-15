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
 * is just a wrapper around the original IXmlTag
 */
public class XmlTagDeclaration: IXmlTag, IDeclaration
{
    private readonly IXmlTag owner;

    public XmlTagDeclaration(IXmlTag owner, IDeclaredElement declaredElement, string declaredName)
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

    public TReturn AcceptVisitor<TContext, TReturn>(IXmlTreeVisitor<TContext, TReturn> visitor, TContext context) =>
        owner.AcceptVisitor(visitor, context);

    public XmlTokenTypes XmlTokenTypes => owner.XmlTokenTypes;

    public IXmlTag GetTag(Predicate<IXmlTag> predicate) => owner.GetTag(predicate);

    public TreeNodeEnumerable<T> GetTags<T>() where T : class, IXmlTag => owner.GetTags<T>();

    public TreeNodeCollection<T> GetTags2<T>() where T : class, IXmlTag => owner.GetTags2<T>();

    public IList<T> GetNestedTags<T>(string xpath) where T : class, IXmlTag => owner.GetNestedTags<T>(xpath);

    public TXmlTag AddTagBefore<TXmlTag>(TXmlTag tag, IXmlTag anchor) where TXmlTag : class, IXmlTag =>
        owner.AddTagBefore(tag, anchor);

    public TXmlTag AddTagAfter<TXmlTag>(TXmlTag tag, IXmlTag anchor) where TXmlTag : class, IXmlTag =>
    owner.AddTagAfter(tag, anchor);

    public void RemoveTag(IXmlTag tag) => owner.RemoveTag(tag);

    public TreeNodeCollection<IXmlTag> InnerTags => owner.InnerTags;

    public TXmlAttribute AddAttributeBefore<TXmlAttribute>(TXmlAttribute attribute, IXmlAttribute anchor)
        where TXmlAttribute : class, IXmlAttribute =>
        owner.AddAttributeBefore(attribute, anchor);

    public TXmlAttribute AddAttributeAfter<TXmlAttribute>(TXmlAttribute attribute, IXmlAttribute anchor)
        where TXmlAttribute : class, IXmlAttribute =>
        owner.AddAttributeAfter(attribute, anchor);

    public void RemoveAttribute(IXmlAttribute attribute) => owner.RemoveAttribute(attribute);

    public IXmlTagHeader Header => owner.Header;

    public IXmlTagFooter Footer => owner.Footer;

    public bool IsEmptyTag => owner.IsEmptyTag;

    public ITreeRange InnerXml => owner.InnerXml;

    public TreeNodeCollection<IXmlToken> InnerTextTokens => owner.InnerTextTokens;

    public string InnerText => owner.InnerText;

    public string InnerValue => owner.InnerValue;

    public IXmlTagHeader HeaderNode => owner.HeaderNode;

    public IXmlTagFooter FooterNode => owner.FooterNode;

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