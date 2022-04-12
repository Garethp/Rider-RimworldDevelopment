using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;

namespace ReSharperPlugin.RimworldDev;

[Language(typeof(XmlLanguage))]
public class RimworldXMLItemProvider: ItemsProviderOfSpecificContext<XmlCodeCompletionContext>
{
    protected override bool IsAvailable(XmlCodeCompletionContext context)
    {
        if (context.TreeNode is XmlIdentifier identifier && identifier.Parent is XmlTagHeaderNode) return true;
        return false;
    }

    protected override bool AddLookupItems(XmlCodeCompletionContext context, IItemsCollector collector)
    {
        collector.Add(CSharpLookupItemFactory.Instance.CreateTextLookupItem(context.Ranges, "RimworldDefHere", true));
            
        return base.AddLookupItems(context, collector);
    }
}