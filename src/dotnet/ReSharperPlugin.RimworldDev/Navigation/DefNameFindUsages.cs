using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.I18n;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Progress;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Navigation.ContextNavigation;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Navigation.Utils;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Features.Navigation.Core.ContextNavigation.ContextSearches;
using JetBrains.ReSharper.Features.Navigation.Features.FindUsages;
using JetBrains.ReSharper.Features.Navigation.Features.Usages;
using JetBrains.ReSharper.Features.Navigation.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.Navigation;

// [ShellFeaturePart]
public class DefNameFindUsages :
    IFindUsagesContextSearch,
    IUsagesContextSearch,
    IRequestContextSearch,
    IContextSearch
{
    protected virtual ReferencePreferenceKind ReferencePreferenceKind => ReferencePreferenceKind.Default;

    public bool IsAvailable(IDataContext dataContext)
    {
        return this.GetElementCandidates(dataContext, this.ReferencePreferenceKind, true).Any();
    }

    protected IEnumerable<DeclaredElementInstance> GetElementCandidates(
        IDataContext context,
        ReferencePreferenceKind kind,
        bool updateOnly)
    {
        IList<IList<DeclaredElementInstance>> declaredElementInstanceListList = ContextNavigationUtil
            .GetCandidateInstances(context, kind, true).SqueezeCandidatesByTarget(
                (this)
                .GetElementTarget);
        List<DeclaredElementInstance> elementCandidates = new List<DeclaredElementInstance>();
        foreach (IList<DeclaredElementInstance> declaredElementInstanceList in declaredElementInstanceListList)
        {
            ICollection<DeclaredElementInstance> collection = UseTypeParameters(declaredElementInstanceList)
                ? SubstitutionUtil.ApplyGenericNavigationBehaviour(context, declaredElementInstanceList)
                : IgnoreSubstitutions(
                    declaredElementInstanceList);
            elementCandidates.AddRange(collection);
        }

        return elementCandidates;
    }

    private static ICollection<DeclaredElementInstance> IgnoreSubstitutions(
        ICollection<DeclaredElementInstance> candidates)
    {
        return candidates.Select(candidate => new DeclaredElementInstance(candidate.Element)).ToList();
    }


    protected virtual bool UseTypeParameters(IList<DeclaredElementInstance> squeezedCandidate) =>
        squeezedCandidate.First<DeclaredElementInstance>().HasFillTypeParameters();


    public bool IsContextApplicable(IDataContext dataContext) => true;

    protected virtual IEqualityComparer<DeclaredElementInstance> Comparer =>
        EqualityComparer<DeclaredElementInstance>.Default;

    protected virtual Pair<VirtualFileSystemPath, TextRange> GetElementTarget(DeclaredElementInstance candidate) =>
        Pair.Of(VirtualFileSystemPath.GetEmptyPathFor(InteractionContext.SolutionContext), TextRange.InvalidRange);

    public IEnumerable<IRequestContextSearchCandidate> GetCandidates(IDataContext context)
    {
        List<IRequestContextSearchCandidate> candidates = new List<IRequestContextSearchCandidate>();
        var elementCandidates = GetElementCandidates(context, ReferencePreferenceKind, false);
        IEqualityComparer<DeclaredElementInstance> comparer = this.Comparer;
        foreach (IList<DeclaredElementInstance> elementList in elementCandidates
                     .Distinct(comparer).SqueezeCandidatesByTarget(
                         this.GetElementTarget))
        {
            candidates.Add(
                new
                    RequestContextSearchCandidate(
                        elementList, this.Present,
                        IsExecuteImmediately));
        }

        return candidates;
    }

    protected IOccurrence Present(DeclaredElementInstance candidate)
    {
        ContainerDisplayStyle containerDisplayStyle = ContainerDisplayStyle.NoContainer;
        if (!candidate.Substitution.IsEmpty() && candidate.Element is IClrDeclaredElement element)
        {
            ITypeElement containingType = element.GetContainingType();
            if (element is IConstructor)
                containingType = containingType?.GetContainingType();
            if (containingType != null)
            {
                foreach (ITypeParameter typeParameter in (IEnumerable<ITypeParameter>)containingType.TypeParameters)
                {
                    if (candidate.Substitution.HasInDomain(typeParameter))
                    {
                        containerDisplayStyle = ContainerDisplayStyle.ContainingType;
                        break;
                    }
                }

                foreach (ITypeParameter allTypeParameter in containingType.GetContainingType().GetAllTypeParameters())
                {
                    if (candidate.Substitution.HasInDomain(allTypeParameter))
                    {
                        containerDisplayStyle = ContainerDisplayStyle.ContainingTypeWithOuterTypes;
                        break;
                    }
                }
            }
        }

        DeclaredElementInstanceOccurrence instanceOccurrence = new DeclaredElementInstanceOccurrence(candidate);
        instanceOccurrence.PresentationOptions = new OccurrencePresentationOptions()
        {
            LocationStyle = GlobalLocationStyle.None,
            ContainerStyle = containerDisplayStyle,
            TextDisplayStyle = TextDisplayStyle.ContainingType
        };
        return (IOccurrence)instanceOccurrence;
    }


    protected sealed class RequestContextSearchCandidate : IRequestContextSearchCandidate
    {
        private readonly Func<DeclaredElementInstance, IOccurrence> myPresentFunction;

        public RequestContextSearchCandidate(
            ICollection<DeclaredElementInstance> elements,
            Func<DeclaredElementInstance, IOccurrence> presentFunction,
            bool isExecuteImmediately)
        {
            this.Elements = elements;
            this.myPresentFunction = presentFunction;
            this.IsExecuteImmediately = isExecuteImmediately;
        }

        public ICollection<DeclaredElementInstance> Elements { get; }

        public IOccurrence Present() => myPresentFunction(Elements.First());

        public bool IsExecuteImmediately { get; }
    }

    protected virtual bool IsExecuteImmediately => false;

    public SearchRequest CreateSearchRequest(
        IDataContext context,
        IRequestContextSearchCandidate selectedCandidates)
    {
        var list = (selectedCandidates as RequestContextSearchCandidate).Elements;

        ICollection<DeclaredElementInstance> elements = this.Promote((ICollection<DeclaredElementInstance>)list);
        return CreateSearchRequest(context, elements, list);
    }

    protected ICollection<DeclaredElementInstance> Promote(
        ICollection<DeclaredElementInstance> elements)
    {
        List<DeclaredElementInstance> declaredElementInstanceList = new List<DeclaredElementInstance>();
        List<Pair<OverridableMemberInstance, JetHashSet<OverridableMemberInstance>>> alternativesMap =
            new List<Pair<OverridableMemberInstance, JetHashSet<OverridableMemberInstance>>>();
        foreach (DeclaredElementInstance element1 in elements)
        {
            if (element1.Element is IOverridableMember element2)
            {
                using (CompilationContextCookie.OverrideOrCreate(element2.Module.GetContextFromModule()))
                {
                    OverridableMemberInstance overridableMemberInstance =
                        new OverridableMemberInstance(element2, element1.Substitution);
                    JetHashSet<OverridableMemberInstance> jetHashSet = new JetHashSet<OverridableMemberInstance>();
                    alternativesMap.Add(Pair.Of(overridableMemberInstance, jetHashSet));
                    IList<OverridableMemberInstance> rootSuperMembers =
                        overridableMemberInstance.GetRootSuperMembers(false);
                    if (rootSuperMembers.Count > 0)
                        jetHashSet.AddRange(rootSuperMembers);
                    else
                        jetHashSet.Add(overridableMemberInstance);
                }
            }
            else
                declaredElementInstanceList.Add(element1);
        }

        if (alternativesMap.Any())
        {
            Shell.Instance.GetComponent<UITaskExecutor>().FreeThreaded.ExecuteTask(Strings.AnalyzingTargets_Text,
                TaskCancelable.Yes, (Action<IProgressIndicator>)(progressIndicator => ReadLockCookie.Execute(
                    (Action)(() =>
                    {
                        using (Interruption.Current.Add(ProgressIndicatorInterruptionSource.Create(progressIndicator)))
                        {
                            progressIndicator.TaskName = Strings.SearchingForQuasiImplementations_Text;
                            progressIndicator.CurrentItemText = Strings.PressCancelToContinueSearchWithoutQuasi_Text;
                            progressIndicator.Start(alternativesMap.Count);
                            foreach (Pair<OverridableMemberInstance, JetHashSet<OverridableMemberInstance>> pair in
                                     alternativesMap)
                            {
                                using (CompilationContextCookie.GetOrCreate(pair.First.Member.Module
                                           .GetContextFromModule()))
                                {
                                    IList<OverridableMemberInstance> rootSuperMembers =
                                        pair.First.GetRootSuperMembers(true, progressIndicator.CreateSubProgress(1.0));
                                    pair.Second.AddRange(rootSuperMembers);
                                }
                            }
                        }
                    }))));
            foreach (Pair<OverridableMemberInstance, JetHashSet<OverridableMemberInstance>> pair in alternativesMap)
            {
                List<DeclaredElementInstance> list = pair.Second
                    .Where((Func<DeclaredElementInstance, bool>)(a => a.GetContainingType() != null)).ToList();
                if (list.Count != 0)
                    declaredElementInstanceList.AddRange(list);
                else
                    declaredElementInstanceList.Add(pair.First);
            }
        }

        return declaredElementInstanceList;
    }

    public SearchRequest CreateSearchRequest(
        IDataContext context,
        ICollection<DeclaredElementInstance> elements,
        ICollection<DeclaredElementInstance> initialTargets)
    {
        ISearchDomain searchDomain = this.CreateSearchDomain(context);
        SearchDeclaredElementUsagesRequest usagesSearchRequest =
            this.CreateUsagesSearchRequest(context, elements, initialTargets, searchDomain);
        if (usagesSearchRequest == null)
            return (SearchDeclaredElementUsagesRequest)null;
        this.InitializeSearchRequest(usagesSearchRequest, initialTargets);
        return usagesSearchRequest;
    }

    protected virtual SearchDeclaredElementUsagesRequest CreateUsagesSearchRequest(
        IDataContext context,
        ICollection<DeclaredElementInstance> elements,
        ICollection<DeclaredElementInstance> initialTargets,
        ISearchDomain searchDomain)
    {
        return new SearchDeclaredElementUsagesRequest(elements, initialTargets,
            SearchPattern.FIND_USAGES | 
            SearchPattern.FIND_DERIVED_CLASSES_AND_STRUCTS |
            SearchPattern.FIND_DERIVED_INTERFACES |
            SearchPattern.FIND_DERIVED_TYPES |
            SearchPattern.FIND_IMPLEMENTING_MEMBERS | 
            SearchPattern.FIND_MEMBER_USAGES |
            SearchPattern.FIND_IMPLEMENTORS_USAGES |
            SearchPattern.FIND_TEXT_OCCURRENCES |
            SearchPattern.FIND_LATEBOUND_REFERENCES | 
            SearchPattern.FIND_RELATED_ELEMENTS |
            SearchPattern.FIND_CANDIDATES | 
            SearchPattern.FIND_EXPLICITLY_TYPED_USAGES_ONLY, searchDomain);
    }

    private void InitializeSearchRequest(
        SearchDeclaredElementUsagesRequest searchRequest,
        ICollection<DeclaredElementInstance> initialTargets)
    {
        if (!initialTargets.IsSingle() || !SearchParametersOverloads)
            return;
        DeclaredElementInstance declaredElementInstance = initialTargets.Single();
        // if (!(declaredElementInstance.Element is IParameter element) ||
        // !(element.ContainingParametersOwner is IOverridableMember containingParametersOwner) ||
        // !containingParametersOwner.CanBeOverridden() || containingParametersOwner is ICompiledElement)
        // return;
        RichText richText = DeclaredElementPresenter.Format(
            PresentationUtil.GetPresentationLanguage(declaredElementInstance.Element),
            DeclaredElementPresenter.KIND_PRESENTER, declaredElementInstance.Element);
        // if (!containingParametersOwner.IsAbstract)
        // {
        // if (!MessageBox.ShowYesNo(
        // Strings.DoYouWantToFindUsagesOfTheParameter_Text.Format(element.ShortName.NON_LOCALIZABLE(),
        // richText.Text.GetPlural())))
        // return;
        // }

        searchRequest.SearchParametersOverloads = true;
    }

    protected virtual bool SearchParametersOverloads => true;

    protected virtual ISearchDomain CreateSearchDomain(IDataContext context) => SearchDomainContextUtil
        .GetSearchDomainContext(context).GetDefaultDomain().SearchDomain;
}