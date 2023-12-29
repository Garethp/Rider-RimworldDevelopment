using System;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Matchers;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Presentations;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp.AspectLookupItems;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.DeclaredElements;
using JetBrains.ReSharper.Psi.CSharp.ExpectedTypes;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.Media;
using JetBrains.Util.Special;

namespace ReSharperPlugin.RimworldDev;

// Note, this class is mostly copy-pasted from Jetbrains CSharpLookupFactory class, I just made some minor additions
// so that the autocomplete would display a bit nicer

public class RimworldCSharpLookupFactory
{
    private readonly Func<LookupItem<CSharpDeclaredElementInfo>, ILookupItemPresentation>
        myFGetDeclaredElementPresentation = (item =>
        {
            var info = item.Info;
            var nullable = new bool?();
            IUnresolvedDeclaredElement unresolvedDeclaredElement = null;
            var preferredDeclaredElement = info.PreferredDeclaredElement;
            if (preferredDeclaredElement != null)
            {
                var element = preferredDeclaredElement.Element;
                if (element is ITypeParameter)
                    nullable =
                        info.Owner.Services.DeclaredElementDescriptionPresenter.IsDeclaredElementObsolete(element);
                unresolvedDeclaredElement = element as IUnresolvedDeclaredElement;
            }

            var presentation = GetOrCreatePresentation(info, PresenterStyles.DefaultPresenterStyle);
            if (nullable.HasValue)
                presentation.IsObsolete = nullable.Value;
            if (unresolvedDeclaredElement != null)
                presentation.TextColor = unresolvedDeclaredElement.IsDynamic ? JetRgbaColors.Blue : JetRgbaColors.Red;
            return presentation;
        });

    private readonly Func<LookupItem<CSharpDeclaredElementInfo>, ILookupItemPresentation>
        myFGetLocalFunctionPresentationWithoutSignature = item =>
            GetOrCreatePresentation(item.Info, PresenterStyles.DefaultNoParametersPresenter);

    private readonly Func<LookupItem<CSharpDeclaredElementInfo>, ILookupItemBehavior> myFGetDeclaredElementBehavior =
        item => new CSharpDeclaredElementBehavior<CSharpDeclaredElementInfo>(item);

    private readonly Func<LookupItem<CSharpDeclaredElementInfo>, ILookupItemMatcher> myFGetDeclaredElementMatcher =
        item => new DeclaredElementMatcher(item.Info, "@");

    private readonly Func<LookupItem<TypeElementInfo>, ILookupItemBehavior> myFGetTypeElementBehavior =
        item => new CSharpDeclaredElementBehavior<TypeElementInfo>(item);

    private readonly Func<LookupItem<TypeElementInfo>, ILookupItemPresentation> myFGetTypeElementPresentation = item =>
    {
        var info = item.Info;
        var style = info.InsertAngleBrackets
            ? PresenterStyles.TypeElementPresenterStyle
            : PresenterStyles.TypeShortNamePresenterStyle;
        var presentation = GetOrCreatePresentation(info, style);
        presentation.VisualReplaceRangeMarker = info.Ranges.CreateVisualReplaceRangeMarker();
        return presentation;
    };

    private static DeclaredElementPresentation<DeclaredElementInfo> GetOrCreatePresentation(
        [NotNull] DeclaredElementInfo info,
        DeclaredElementPresenterStyle style,
        [CanBeNull] string typeParameterString = null)
    {
        var preferredDeclaredElement = info.PreferredDeclaredElement;
        var name = info.Text;
        if (preferredDeclaredElement != null && preferredDeclaredElement.Element is ICompiledElement element &&
            element.Type() == null)
            return GetPresentationCache(element.GetSolution())
                .GetPresentation(info, element, style, typeParameterString);
        else
            return new DeclaredElementPresentation<DeclaredElementInfo>(info, style, typeParameterString);
    }

    private static LookupItemPresentationCache GetPresentationCache(
        [NotNull] ISolution solution)
    {
        var presentationCache1 = ourPresentationCache;
        if (presentationCache1 != null)
            return presentationCache1;
        var presentationCache2 = ourPresentationCache = solution.GetComponent<LookupItemPresentationCache>();
        solution.GetSolutionLifetimes().MaximumLifetime.OnTermination(() => ourPresentationCache = null);
        return presentationCache2;
    }

    private static LookupItemPresentationCache ourPresentationCache;

    // @TODO: See if we can merge both lookup creations together and just have thin interfaces
    public LookupItem<CSharpDeclaredElementInfo> CreateDeclaredElementLookupItem(
        [NotNull] CSharpCodeCompletionContext context,
        [NotNull] string name,
        [NotNull] DeclaredElementInstance instance,
        bool includeFollowingExpression = true,
        bool bind = false,
        QualifierKind qualifierKind = QualifierKind.NONE)
    {
        Assertion.Assert(instance != null && instance.IsValid(), "instance != null && instance.IsValid()");
        var basicContext = context.BasicContext;
        var replaceRange = context.CompletionRanges.ReplaceRange;

        // @TODO: Find out why we needed to do this
        if (context.NodeInFile.GetText().StartsWith("\""))
        {
            replaceRange = new DocumentRange(replaceRange.Document,
                new TextRange(replaceRange.TextRange.StartOffset + 1, replaceRange.TextRange.EndOffset - 1));
        }

        var completionRange = new TextLookupRanges(replaceRange, replaceRange, null, false);
        // var completionRange = context.CompletionRanges;
        
        var declaredElementInfo = new CSharpDeclaredElementInfo(name, instance, basicContext.LookupItemsOwner, context)
        {
            Bind = bind,
            Ranges = completionRange
        };

        var element = instance.Element;
        if (element is ITypeElement typeElement && typeElement.HasTypeParameters())
        {
            declaredElementInfo.AppendToIdentity(2);
            declaredElementInfo.Placement.OrderString += "<>";
        }

        if (qualifierKind != QualifierKind.NONE)
            declaredElementInfo.QualifierKind = qualifierKind;

        if (element is IField field)
        {
            if (!field.GetContainingType().IsValueTuple())
                declaredElementInfo.Bind = true;
        }
        else if (element is ITypeElement)
        {
            declaredElementInfo.Bind = true;
            if (element is not ITypeParameter)
                declaredElementInfo.IsTypeElement = true;
        }

        switch (element)
        {
            case IExternAlias:
                declaredElementInfo.TailType = CSharpTailType.DoubleColon;
                break;
            case IClass:
            case IStruct:
                declaredElementInfo.Placement.OrderString += " T";
                break;
            case IProperty:
                declaredElementInfo.Placement.OrderString += " P";
                break;
        }

        if (element is not IParametersOwner declaredElement)
            declaredElement = (element as IDelegate).IfNotNull(_ => _.InvokeMethod);

        if (declaredElement != null && (context.BasicContext.ShowSignatures || element is IUnresolvedDeclaredElement) &&
            declaredElement.Parameters.Count > 0 && !declaredElement.IsCSharpProperty())
        {
            var stringBuilder = new StringBuilder();
            var parameters = declaredElement.Parameters;
            for (var index = 0; index < parameters.Count; ++index)
            {
                var type = parameters[index].Type;
                stringBuilder.Append((parameters[index].IsParameterArray ? "params" : string.Empty) +
                                     type.GetPresentableName(CSharpLanguage.Instance));
                if (index < parameters.Count - 1)
                    stringBuilder.Append(", ");
            }

            declaredElementInfo.Placement.OrderString = name + "(" + stringBuilder + ")";
        }

        var fGetPresentation = myFGetDeclaredElementPresentation;
        if (element is ILocalFunction && !context.BasicContext.ShowSignatures)
            fGetPresentation = myFGetLocalFunctionPresentationWithoutSignature;
        var dataHolder = LookupItemFactory.CreateLookupItem(declaredElementInfo)
            .WithPresentation(fGetPresentation)
            .WithBehavior(myFGetDeclaredElementBehavior)
            .WithMatcher(myFGetDeclaredElementMatcher);

        var typeOwner = element as ITypeOwner;
        if (instance.Element is IAnonymousTypeProperty)
            declaredElementInfo.HighlightSameType = true;
        IDelegate @delegate = null;
        if (typeOwner != null && GetType(typeOwner) is IDeclaredType type1)
            @delegate = type1.GetTypeElement() as IDelegate;
        if (element is ITypeElement || element is INamespace)
            dataHolder.PutKey(CompletionKeys.IsTypeOrNamespaceKey);

        if (name == "hediff")
        {
            var displayText = dataHolder.DisplayTypeName;
        }

        return dataHolder;
    }

    public LookupItem<CSharpDeclaredElementInfo> CreateDeclaredElementLookupItem(
        [NotNull] RimworldXmlCodeCompletionContext context,
        [NotNull] string name,
        [NotNull] DeclaredElementInstance instance,
        bool includeFollowingExpression = true,
        bool bind = false,
        QualifierKind qualifierKind = QualifierKind.NONE)
    {
        Assertion.Assert(instance != null && instance.IsValid(), "instance != null && instance.IsValid()");
        var basicContext = context.BasicContext;
        var declaredElementInfo = new CSharpDeclaredElementInfo(name, instance, basicContext.LookupItemsOwner, context)
        {
            Bind = bind,
            Ranges = context.Ranges
        };

        var element = instance.Element;
        if (element is ITypeElement typeElement && typeElement.HasTypeParameters())
        {
            declaredElementInfo.AppendToIdentity(2);
            declaredElementInfo.Placement.OrderString += "<>";
        }

        if (qualifierKind != QualifierKind.NONE)
            declaredElementInfo.QualifierKind = qualifierKind;

        if (element is IField field)
        {
            if (!field.GetContainingType().IsValueTuple())
                declaredElementInfo.Bind = true;
        }
        else if (element is ITypeElement)
        {
            declaredElementInfo.Bind = true;
            if (element is not ITypeParameter)
                declaredElementInfo.IsTypeElement = true;
        }

        switch (element)
        {
            case IExternAlias:
                declaredElementInfo.TailType = CSharpTailType.DoubleColon;
                break;
            case IClass:
            case IStruct:
                declaredElementInfo.Placement.OrderString += " T";
                break;
            case IProperty:
                declaredElementInfo.Placement.OrderString += " P";
                break;
        }

        if (element is not IParametersOwner declaredElement)
            declaredElement = (element as IDelegate).IfNotNull(_ => _.InvokeMethod);

        if (declaredElement != null && (context.BasicContext.ShowSignatures || element is IUnresolvedDeclaredElement) &&
            declaredElement.Parameters.Count > 0 && !declaredElement.IsCSharpProperty())
        {
            var stringBuilder = new StringBuilder();
            var parameters = declaredElement.Parameters;
            for (var index = 0; index < parameters.Count; ++index)
            {
                var type = parameters[index].Type;
                stringBuilder.Append((parameters[index].IsParameterArray ? "params" : string.Empty) +
                                     type.GetPresentableName(CSharpLanguage.Instance));
                if (index < parameters.Count - 1)
                    stringBuilder.Append(", ");
            }

            declaredElementInfo.Placement.OrderString = name + "(" + stringBuilder + ")";
        }

        var fGetPresentation = myFGetDeclaredElementPresentation;
        if (element is ILocalFunction && !context.BasicContext.ShowSignatures)
            fGetPresentation = myFGetLocalFunctionPresentationWithoutSignature;
        var dataHolder = LookupItemFactory.CreateLookupItem(declaredElementInfo)
            .WithPresentation(fGetPresentation)
            .WithBehavior(myFGetDeclaredElementBehavior)
            .WithMatcher(myFGetDeclaredElementMatcher);

        var typeOwner = element as ITypeOwner;
        if (instance.Element is IAnonymousTypeProperty)
            declaredElementInfo.HighlightSameType = true;
        IDelegate @delegate = null;
        if (typeOwner != null && GetType(typeOwner) is IDeclaredType type1)
            @delegate = type1.GetTypeElement() as IDelegate;
        if (element is ITypeElement || element is INamespace)
            dataHolder.PutKey(CompletionKeys.IsTypeOrNamespaceKey);

        if (name == "hediff")
        {
            var displayText = dataHolder.DisplayTypeName;
        }

        return dataHolder;
    }

    [CanBeNull]
    private static IType GetType([NotNull] ITypeOwner typeOwner)
    {
        if (!(typeOwner is ILocalVariableDeclaration variableDeclaration) || !variableDeclaration.IsVar)
            return typeOwner.Type;
        return !(variableDeclaration is IManagedVariableImpl managedVariableImpl)
            ? null
            : managedVariableImpl.CachedType;
    }
}