using JetBrains.ReSharper.Psi;
using JetBrains.UI.Icons;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

/**
 * For some reason we need a DeclaredElementType for our IDeclaredElement
 */
public class XmlTagDeclaredElemntType : DeclaredElementType
{
    public static readonly DeclaredElementType Instance = new XmlTagDeclaredElemntType();

    private XmlTagDeclaredElemntType()
        : base("XmlTag")
    {
    }

    public override string PresentableName => "XmlTag";

    public override IconId GetImage() => null;

    protected override IDeclaredElementPresenter DefaultPresenter => XmlTagDeclaredElementPresenter.Instance;

    public override bool IsPresentable(PsiLanguageType language) => false;
}