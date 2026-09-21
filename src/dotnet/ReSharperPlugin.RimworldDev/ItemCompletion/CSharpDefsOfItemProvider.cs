using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.RimworldDev.SymbolScope;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace ReSharperPlugin.RimworldDev.ItemCompletion;

[Language(typeof(CSharpLanguage))]
public class CSharpDefsOfItemProvider : ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
{
    private static RimworldCSharpLookupFactory LookupFactory = new();

    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
        var node = context.NodeInFile;
        if (!node.Language.IsLanguage(CSharpLanguage.Instance)) return false;
        if (GetFieldType(node) is null) return false;
        if (node.Parent is not ICSharpTypeMemberDeclaration memberDeclaration) return false;
        if (memberDeclaration.GetContainingTypeElement() is not IClass containingClass) return false;
        if (containingClass
                .GetAttributeInstances(AttributesSource.Self)
                .FirstOrDefault(attribute => attribute.GetClrName().FullName == "RimWorld.DefOf") is null) return false;

        return true;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
    {
        var node = context.NodeInFile;

        if (GetFieldType(node) is not { } fieldType) return false;

        var defTypeName = fieldType.GetClrName().ShortName;
        var xmlSymbolTable = context.NodeInFile.GetSolution().GetComponent<RimworldSymbolScope>();

        var allDefs = xmlSymbolTable.GetDefsByType(defTypeName);

        foreach (var key in allDefs)
        {
            var defType = key.Split('/').First();
            var defName = key.Split('/').Last();

            var item = xmlSymbolTable.GetTagByDef(defType, defName);

            var lookup = LookupFactory.CreateDeclaredElementLookupItem(context, defName,
                new DeclaredElementInstance(new XMLTagDeclaredElement(item, defType, defName, false)));
            collector.Add(lookup);
        }
        
        return base.AddLookupItems(context, collector);
    }

    private static IDeclaredType GetFieldType(ITreeNode node) => node.Parent switch
    {
        // We have two possibilities we need to account for: With a `;` already in place and without one. With it in
        // place, Rider reports that it's a IFieldDeclaration. Without it, it'll report that it's a IMethodDeclaration.
        // In the case that there's no trailing `;` and Rider thinks it's a method Declaration, we can also check that
        // there's no left parenthesis, which would be the opening of the signature for the method. If there's not,
        // we'll just keep treating it like a field.
        IFieldDeclaration fieldDeclaration => fieldDeclaration.Type as IDeclaredType,
        IMethodDeclaration { LPar: null } methodDeclaration when methodDeclaration.NameIdentifier == node =>
            methodDeclaration.Type as IDeclaredType,
        _ => null
    };
}