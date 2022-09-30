using System.Text;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

  public class XmlTagDeclaredElementPresenter : IDeclaredElementPresenter
  {
    [NotNull]
    public static readonly IDeclaredElementPresenter Instance = new XmlTagDeclaredElementPresenter();

    public virtual RichText Format(
      DeclaredElementPresenterStyle style,
      IDeclaredElement element,
      ISubstitution substitution,
      out DeclaredElementPresenterMarking marking)
    {
      marking = new DeclaredElementPresenterMarking();
      XMLTagDeclaredElement pathDeclaredElement = (XMLTagDeclaredElement) element;
      StringBuilder builder = new StringBuilder();
      if (style.ShowEntityKind != EntityKindForm.NONE)
        FormatEntityKind(element, style, marking, builder);
      if (style.ShowName != NameStyle.NONE)
      {
        if (style.ShowNameInQuotes)
          builder.Append('"');
        marking.NameRange = AppendString(builder, pathDeclaredElement.ShortName);
        if (style.ShowNameInQuotes)
          builder.Append('"');
      }
      return builder.ToString();
    }

    protected virtual void FormatEntityKind(
      IDeclaredElement declaredElement,
      DeclaredElementPresenterStyle style,
      DeclaredElementPresenterMarking marking,
      StringBuilder builder)
    {
      bool flag = style.ShowEntityKind == EntityKindForm.NORMAL_IN_BRACKETS;
      string entityKind = GetEntityKind(declaredElement);
      marking.EntityKindRange = AppendString(builder, flag ? "[" + entityKind + "] " : entityKind + " ");
    }

    protected static TextRange AppendString([NotNull] StringBuilder builder, [NotNull] string text)
    {
      int length = builder.Length;
      builder.Append(text);
      return text.Length == 0 ? TextRange.InvalidRange : new TextRange(length, builder.Length);
    }

    public virtual string Format(ParameterKind parameterKind) => string.Empty;

    public virtual string Format(AccessRights accessRights) => string.Empty;

    public virtual string GetEntityKind(IDeclaredElement declaredElement) => !(declaredElement is XMLTagDeclaredElement) ? "" : "XmlTag";
  }