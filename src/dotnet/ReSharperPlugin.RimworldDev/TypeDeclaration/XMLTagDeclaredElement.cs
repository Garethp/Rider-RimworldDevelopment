using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

/**
 * This class allows us to create an IDeclaredElement out of an IXmlTag, allowing us to use it as an IReference
 */
public class XMLTagDeclaredElement: IDeclaredElement
{
    public XMLTagDeclaredElement(IXmlTag owner, string shortName, bool caseSensitiveName)
    {
        this.owner = owner;
        myPsiServices = owner.GetPsiServices();
        ShortName = shortName;
        CaseSensitiveName = caseSensitiveName;
        PresentationLanguage = owner.Language;
    }

    private readonly IXmlTag owner;
    
    public string ShortName { get; }

    public bool CaseSensitiveName { get; }

    public PsiLanguageType PresentationLanguage { get; }
     
    private readonly IPsiServices myPsiServices;
    
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

        return new List<IDeclaration> {new XmlTagDeclaration(owner, this, ShortName)};
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

    public XmlNode GetXMLDoc(bool inherit) => (XmlNode) null;

    public XmlNode GetXMLDescriptionSummary(bool inherit) => (XmlNode) null;

}