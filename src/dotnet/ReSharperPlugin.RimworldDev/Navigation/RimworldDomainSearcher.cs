using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.RimworldDev.Navigation;

public class RimworldDomainSearcher : IDomainSpecificSearcher
{
    private readonly IDomainSpecificSearcherFactory _factory;
    private readonly IDeclaredElementsSet _elements;

    public RimworldDomainSearcher(
        IDomainSpecificSearcherFactory factory,
        IDeclaredElementsSet elements
    )
    {
        _factory = factory;
        _elements = elements;
    }

    public bool ProcessProjectItem<TResult>(IPsiSourceFile sourceFile, IFindResultConsumer<TResult> consumer)
    {
        return true;
    }

    public bool ProcessElement<TResult>(ITreeNode element, IFindResultConsumer<TResult> consumer)
    {
        return true;
    }
}