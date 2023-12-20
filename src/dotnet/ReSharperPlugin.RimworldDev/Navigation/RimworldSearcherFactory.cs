using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Html;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Web.Searching;
using JetBrains.ReSharper.Psi.Web.Util;
using JetBrains.ReSharper.Psi.Xml;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace ReSharperPlugin.RimworldDev.Navigation;

[PsiSharedComponent]
public class RimworldSearcherFactory : DomainSpecificSearcherFactoryBase
{
    private readonly SearchDomainFactory mySearchDomainFactory;

    public RimworldSearcherFactory(SearchDomainFactory searchDomainFactory) =>
        mySearchDomainFactory = searchDomainFactory;


    public override bool IsCompatibleWithLanguage(PsiLanguageType languageType) => languageType.Is<XmlLanguage>();

    public override ISearchDomain GetDeclaredElementSearchDomain(IDeclaredElement declaredElement)
    {
        return this.mySearchDomainFactory.CreateSearchDomain(declaredElement.GetSolution(), false);
    }

    public override IDomainSpecificSearcher CreateReferenceSearcher(
        IDeclaredElementsSet elements,
        ReferenceSearcherParameters referenceSearcherParameters)
    {
        // if (SearcherUtil.OnlyNonWebElementsInList(elements, false))
        // return null;

        return new CustomSearcher<XmlLanguage>(this,
            elements, referenceSearcherParameters, false);
    }

    // public override IDomainSpecificSearcher CreateLateBoundReferenceSearcher(
    //     IDeclaredElementsSet elements,
    //     ReferenceSearcherParameters referenceSearcherParameters)
    // {
    //     if (elements.First() is XMLTagDeclaredElement xmlTagDeclaredElement)
    //         xmlTagDeclaredElement.SwapShortName();
    //
    //     // if (SearcherUtil.OnlyNonWebElementsInList(elements, false))
    //     //     return null;
    //     return new WebReferenceSearcher<XmlLanguage>(this,
    //         elements,
    //         new ReferenceSearcherParameters(
    //             referenceSearcherParameters.OriginalElements,
    //             true), true);
    //
    //
    //     // return new RimworldDomainSearcher(this, elements);
    // }

    public override IEnumerable<string> GetAllPossibleWordsInFile(IDeclaredElement element)
    {
        return new List<string> { element.ShortName.Split('/').Last() };
        // return base.GetAllPossibleWordsInFile(element);
    }
}