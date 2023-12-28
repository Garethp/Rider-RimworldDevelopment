using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

/**
 * This class allows us to create an IDeclaredElement out of an IXmlTag, allowing us to use it as an IReference
 */
public class XMLTagDeclaredElement : IDeclaredElement
{
    public XMLTagDeclaredElement(ITreeNode owner, string defType, string defName, bool caseSensitiveName)
    {
        this.owner = owner;
        myPsiServices = owner.GetPsiServices();
        ShortName = $"{defType}/{defName}";
        CaseSensitiveName = caseSensitiveName;
        PresentationLanguage = owner.Language;
    }

    public void Update(ITreeNode newOwner)
    {
        owner = newOwner;
        myPsiServices = newOwner.GetPsiServices();
    }

    private ITreeNode owner;
    
    public string ShortName { get; }

    public bool CaseSensitiveName { get; }

    public PsiLanguageType PresentationLanguage { get; }

    private IPsiServices myPsiServices;

    public DeclaredElementType GetElementType() => XmlTagDeclaredElemntType.Instance;

    public bool IsValid() => true;

    public bool IsSynthetic() => false;

    public IList<IDeclaration> GetDeclarations()
    {
        return new List<IDeclaration> { new XmlTagDeclaration(owner, this, ShortName) };
    }

    public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
    {
        if (!HasDeclarationsIn(sourceFile)) return EmptyList<IDeclaration>.Instance;

        return new List<IDeclaration> { new XmlTagDeclaration(owner, this, ShortName) };
    }

    public HybridCollection<IPsiSourceFile> GetSourceFiles()
    {
        if (owner.GetSourceFile() is not { } sourceFile) return HybridCollection<IPsiSourceFile>.Empty;
        
        return new HybridCollection<IPsiSourceFile>(sourceFile);
    }

    public bool HasDeclarationsIn(IPsiSourceFile sourceFile)
    {
        return sourceFile.Equals(owner.GetSourceFile());
    }

    public IPsiServices GetPsiServices() => myPsiServices;

    public XmlNode GetXMLDoc(bool inherit) => (XmlNode)null;

    public XmlNode GetXMLDescriptionSummary(bool inherit) => (XmlNode)null;
}