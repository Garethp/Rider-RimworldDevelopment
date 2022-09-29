using System.Linq;
using JetBrains.Annotations;
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

namespace ReSharperPlugin.RimworldDev.References;

public class RimworldXmlDefReference : 
    XmlReferenceWithTokenBase<ITreeNode>
{
    private readonly IXmlTag myTypeElement;

    public RimworldXmlDefReference(ITreeNode owner, IXmlToken token, TreeTextRange rangeWithin)
        : base(owner, token, rangeWithin)
    {
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        throw new System.NotImplementedException();
    }
}