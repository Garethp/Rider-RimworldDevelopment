using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Conversions;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.TypeDeclaration;

/**
 * I don't really remember what this is doing or how it works.
 *
 * @TODO: Re-read and document this
 */
public class RimworldXmlReference : 
    TreeReferenceBase<ITreeNode>,
    ICompletableReference,
    IReference,
    IUserDataHolder
{
    private readonly ISymbolFilter myExactNameFilter;
    private readonly ISymbolFilter myPropertyFilter;

    private readonly IDeclaredElement myTypeElement;

    public RimworldXmlReference(IDeclaredElement typeElement, [NotNull] ITreeNode owner)
        : base(owner)
    {
        myTypeElement = typeElement;
        myExactNameFilter = new ExactNameFilter(myOwner.GetText());
        myPropertyFilter = new PredicateFilter(FilterToApplicableProperties);
    }

    private static bool FilterToApplicableProperties([NotNull] ISymbolInfo symbolInfo)
    {
        if (!(symbolInfo.GetDeclaredElement() is ITypeMember declaredElement))
            return false;
        var predefinedType = declaredElement.Module.GetPredefinedType();
        if (!(declaredElement is IProperty property) || property.GetAccessRights() != AccessRights.PUBLIC || !property.IsStatic)
            return false;
        var typeConversionRule = declaredElement.Module.GetTypeConversionRule();
        var typeElement = predefinedType.GenericIEnumerable.GetTypeElement();
        if (typeElement == null)
            return false;
        var arrayType = TypeFactory.CreateArrayType(predefinedType.Object, 1, NullableAnnotation.Unknown);
        var type = EmptySubstitution.INSTANCE.Extend(typeElement.TypeParameters[0], arrayType).Apply(predefinedType.GenericIEnumerable);
        return property.Type.IsImplicitlyConvertibleTo(type, typeConversionRule);
    }
    
    public override string GetName() => myTypeElement.ShortName;
    private string GetShortName() => GetName().Split('.').Last();
    
    public override TreeTextRange GetTreeTextRange() => myOwner.GetTreeTextRange();

    public override IAccessContext GetAccessContext() => new ElementAccessContext(myOwner);

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        ISymbolTable table;
        if (myTypeElement is IClass @class) {
            table = ResolveUtil
                .GetSymbolTableByNamespace(@class.GetContainingNamespace(), @class.Module, true)
                .Distinct();
        }
        else if (myTypeElement is IField field)
        {
            table = ResolveUtil
                .GetSymbolTableByTypeElement(field.GetContainingType(), SymbolTableMode.FULL, field.Module)
                .Distinct();
        }
        else if (myTypeElement is IEnum enumerator)
        {
            table = ResolveUtil
                .GetSymbolTableByTypeElement(enumerator, SymbolTableMode.FULL, enumerator.Module)
                .Distinct();
        }
        else
        {
            return EmptySymbolTable.INSTANCE;
        }
        
        if (!useReferenceName)
            return table;

        return table.Filter(myTypeElement.ShortName, new AllFilter(myTypeElement.ShortName));
    }

    public ISymbolTable GetCompletionSymbolTable() => GetReferenceSymbolTable(false);

    public override IReference BindTo(IDeclaredElement element) => BindTo(element, EmptySubstitution.INSTANCE);

    public override IReference BindTo(
        IDeclaredElement element,
        ISubstitution substitution)
    {
        return this;
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
        ResolveResultWithInfo resolveResult = GetReferenceSymbolTable(true).GetResolveResult(myTypeElement.ShortName);
        return resolveResult;
    }
    
    public sealed class AllFilter : SimpleSymbolInfoFilter
    {
        [NotNull]
        private readonly string myName;

        public AllFilter([NotNull] string name) => myName = name;

        public override bool Accepts(ISymbolInfo info) => true;

        public override ResolveErrorType ErrorType => ResolveErrorType.WRONG_NAME_CASE;

        public override FilterRunType RunType => FilterRunType.MUST_RUN_NO_CANDIDATES;
    }
}