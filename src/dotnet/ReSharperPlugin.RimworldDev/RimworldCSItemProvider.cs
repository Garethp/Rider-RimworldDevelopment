using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace ReSharperPlugin.RimworldDev;

// TODO: Check if this is even needed, then remove?

// I *think* this was a testing proof of concept for creating lookup items, and it was easier to go by existing examples
// for adding to C#  code than figuring out why the hell Rider wasn't doing anything for adding to XML

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