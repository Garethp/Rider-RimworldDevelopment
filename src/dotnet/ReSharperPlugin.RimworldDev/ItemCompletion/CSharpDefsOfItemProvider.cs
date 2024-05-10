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
        if (node.Parent is not IFieldDeclaration fieldDeclaration) return false;
        if (fieldDeclaration.Type is not IDeclaredType) return false;
        if (fieldDeclaration.GetContainingTypeElement() is not IClass containingClass) return false;
        if (containingClass
                .GetAttributeInstances(AttributesSource.Self)
                .FirstOrDefault(attribute => attribute.GetClrName().FullName == "RimWorld.DefOf") is null) return false;

        return true;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
    {
        var node = context.NodeInFile;

        if (node.Parent is not IFieldDeclaration fieldDeclaration) return false;
        if (fieldDeclaration.Type is not IDeclaredType fieldType) return false;

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
}