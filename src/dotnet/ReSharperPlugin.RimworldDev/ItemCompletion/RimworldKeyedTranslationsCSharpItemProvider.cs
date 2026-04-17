using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using ReSharperPlugin.RimworldDev.SymbolScope;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace ReSharperPlugin.RimworldDev.ItemCompletion;

[Language(typeof(CSharpLanguage))]
public class RimworldKeyedTranslationsCSharpItemProvider: ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
{
    private static RimworldCSharpLookupFactory LookupFactory = new();
    
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
        var node = context.NodeInFile;
        if (!node.Language.IsLanguage(CSharpLanguage.Instance)) return false;
        if (node is not CSharpGenericToken) return false;
        if (node.NodeType != CSharpTokenType.STRING_LITERAL_REGULAR) return false;
        
        return true;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
    {
        var node = context.NodeInFile;

        if (node.Parent?.NextSibling?.NextSibling is not ICSharpIdentifier identifier ||
            identifier.GetText() != "Translate")
            return false;
        
        var xmlSymbolTable = context.NodeInFile.GetSolution().GetComponent<RimworldKeyedTranslationSymbolScope>();
        
        foreach (var key in xmlSymbolTable.GetKeys())
        {
            var keyTag = xmlSymbolTable.GetKeyTag(key);
            if (keyTag == null) continue;
            
            var lookup = LookupFactory.CreateDeclaredElementLookupItem(
                context,
                key,
                new DeclaredElementInstance(new XMLTagDeclaredElement(keyTag, $"English/{key}", false))
            );
            
            collector.Add(lookup);
        }
        
        return base.AddLookupItems(context, collector);
    }

}