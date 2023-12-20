using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Finder;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.RimworldDev.Navigation;

public class CustomSearcher<TLanguage> : IDomainSpecificSearcher where TLanguage : KnownLanguage
{
    private readonly JetHashSet<string> myNames;
    private readonly JetHashSet<string> myWordsInText;
    private readonly IDeclaredElementsSet myElements;
    private readonly ReferenceSearcherParameters myReferenceSearcherParameters;
    private readonly bool mySearchForLateBound;
    private readonly IWordIndex myWordIndex;

    public CustomSearcher(
        IDomainSpecificSearcherFactory factory,
        IDeclaredElementsSet elements,
        ReferenceSearcherParameters referenceSearcherParameters,
        bool searchForLateBound)
    {
        mySearchForLateBound = searchForLateBound;
        myElements = elements;
        myReferenceSearcherParameters = referenceSearcherParameters;
        myNames = [];
        myWordsInText = [];

        // The element shortNames are {defType}/{defName} to make them unique references, however in the text they're
        // just {defName} so we split up their shortName for seraching
        foreach (var element in myElements)
        {
            myNames.Add(element.ShortName.Split('/').Last());
            myWordsInText.UnionWith(factory.GetAllPossibleWordsInFile(element));
        }

        myWordIndex = myElements.First().GetPsiServices().WordIndex;
    }

    public bool ProcessProjectItem<TResult>(
        IPsiSourceFile sourceFile,
        IFindResultConsumer<TResult> consumer)
    {
        if (!CanContainWord(sourceFile)) return false;

        foreach (ITreeNode psiFile in sourceFile.GetPsiFiles<TLanguage>())
        {
            if (ProcessElement(psiFile, consumer))
                return true;
        }

        return false;
    }

    private bool CanContainWord([NotNull] IPsiSourceFile sourceFile)
    {
        return myWordsInText.Any(word => myWordIndex.CanContainWord(sourceFile, word));
    }

    public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
    {
        Assertion.Assert(element != null);
        JetHashSet<string> referenceNames = [];
        foreach (var myElement in myElements)
        {
            myNames.Add(myElement.ShortName);
        }

        return (!mySearchForLateBound
            ? new ReferenceSearchSourceFileProcessor<TResult>(element, myReferenceSearcherParameters, consumer,
                myElements, myWordsInText, referenceNames)
            : (NamedThingsSearchSourceFileProcessor)new LateBoundReferenceSourceFileProcessor<TResult>(element,
                consumer, myElements, myWordsInText, referenceNames)).Run() == FindExecution.Stop;
    }
}