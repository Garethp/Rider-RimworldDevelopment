using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace ReSharperPlugin.RimworldDev;

[Language(typeof(CSharpLanguage))]
public class RimworldCSharpItemProvider: ItemsProviderOfSpecificContext<CSharpCodeCompletionContext>
{
    protected override bool IsAvailable(CSharpCodeCompletionContext context)
    {
        return true;
    }

    protected override bool AddLookupItems(CSharpCodeCompletionContext context, IItemsCollector collector)
    {
        collector.Add(CSharpLookupItemFactory.Instance.CreateTextLookupItem(context.CompletionRanges, "RimworldDefHere", true));
            
        return base.AddLookupItems(context, collector);
    }
}