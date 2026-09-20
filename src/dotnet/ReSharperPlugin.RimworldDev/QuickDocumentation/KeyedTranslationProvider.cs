using System;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Components.Theming;
using JetBrains.ReSharper.Feature.Services.Descriptions;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.QuickDoc;
using JetBrains.ReSharper.Feature.Services.QuickDoc.Render;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.UI.RichText;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace DefaultNamespace;

[QuickDocProvider(1)]
public class KeyedTranslationProvider(ITheming theming): IQuickDocProvider
{
    public bool CanNavigate(IDataContext context)
    {
        var data = context.GetData(PsiDataConstants.DECLARED_ELEMENTS_FROM_ALL_CONTEXTS);
        var tags = data?.Where(element => element is XMLTagDeclaredElement).ToList();

        return tags != null && tags.Count != 0;
    }

    public void Resolve(IDataContext context, Action<IQuickDocPresenter, PsiLanguageType> resolved)
    {
        var data = context.GetData(PsiDataConstants.DECLARED_ELEMENTS_FROM_ALL_CONTEXTS);
        var tags = data?.Where(element => element is XMLTagDeclaredElement).ToList();

        var presenter =
            new KeyedTranslationPresenter(theming, tags.First());

        resolved(presenter, CSharpLanguage.Instance);
    }
}

public class KeyedTranslationPresenter(ITheming theming, IDeclaredElement element) : IQuickDocPresenter
{
    private readonly DeclaredElementEnvoy<IDeclaredElement> myEnvoy = new(element);

    public QuickDocTitleAndText GetHtml(PsiLanguageType presentationLanguage)
    {
        var validDeclaredElement = myEnvoy.GetValidDeclaredElement();
        if (validDeclaredElement == null)
            return QuickDocTitleAndText.Empty;
        var presenter = new KeyedTranslationDescriptionProvider();
        var block = presenter.GetElementDescription(
            validDeclaredElement, 
            DeclaredElementDescriptionStyle.FULL_STYLE,
            presentationLanguage
        );
        
        return new QuickDocTitleAndText(
            new RichText().FullHtml(_ => { }, body => body.Append(block.ToHtml()), theming),
            DeclaredElementPresenter.Format(
                presentationLanguage,
                DeclaredElementPresenter.FULL_NESTED_NAME_PRESENTER,
                validDeclaredElement
            )
        );
    }

    public string GetId() => null;

    public IQuickDocPresenter Resolve(string id) => null;

    public void OpenInEditor(string navigationId = "")
    {
        IDeclaredElement validDeclaredElement = this.myEnvoy.GetValidDeclaredElement();
        if (validDeclaredElement == null)
            return;
        validDeclaredElement.Navigate(true);
    }

    public void ReadMore(string navigationId = "")
    {
    }
}

public class KeyedTranslationDescriptionProvider : IDeclaredElementDescriptionProvider
{
    public RichTextBlock GetElementDescription(IDeclaredElement element, DeclaredElementDescriptionStyle style,
        PsiLanguageType language, IPsiModule module = null)
    {
        if (element is not XMLTagDeclaredElement declaredElement) return new RichTextBlock();

        var block = new RichTextBlock { declaredElement.GetText() };

        return block;
    }

    public bool? IsElementObsolete(IDeclaredElement element, out RichTextBlock obsoleteDescription,
        DeclaredElementDescriptionStyle style)
    {
        obsoleteDescription = null;
        return false;
    }

    public int Priority => 0;
}