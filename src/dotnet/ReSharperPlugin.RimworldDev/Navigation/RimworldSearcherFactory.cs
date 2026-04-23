using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Xml;

namespace ReSharperPlugin.RimworldDev.Navigation;

[PsiSharedComponent]
public class RimworldSearcherFactory(SearchDomainFactory searchDomainFactory) : DomainSpecificSearcherFactoryBase
{
    public override bool IsCompatibleWithLanguage(PsiLanguageType languageType) => 
        languageType.Is<XmlLanguage>() || languageType.Is<CSharpLanguage>();

    public override ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
    {
        return searchDomainFactory.CreateSearchDomain(declaredElement.GetSolution(), false);
    }

    public override IDomainSpecificSearcher CreateReferenceSearcher(
        IDeclaredElementsSet elements,
        ReferenceSearcherParameters referenceSearcherParameters)
    {
        elements = new DeclaredElementsSet(elements.Where(element => element.PresentationLanguage.Is<XmlLanguage>()));
        
        return new CustomSearcher(this, elements, referenceSearcherParameters, false);
    }

    public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
    {
        return new List<string> { element.ShortName.Split('/').Last() };
    }
}