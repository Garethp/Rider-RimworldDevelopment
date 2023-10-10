using System.Collections.Generic;
using System.Linq;
using JetBrains;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace ReSharperPlugin.RimworldDev.SymbolScope;

[PsiComponent]
public class RimworldSymbolScope : SimpleICache<List<RimworldXmlDefSymbol>>
{
    public Dictionary<string, ITreeNode> DefTags = new();

    public RimworldSymbolScope
    (Lifetime lifetime, [NotNull] IShellLocks locks, [NotNull] IPersistentIndexManager persistentIndexManager,
        long? version = null)
        : base(lifetime, locks, persistentIndexManager, RimworldXmlDefSymbol.Marshaller, version)
    {
    }

    protected override bool IsApplicable(IPsiSourceFile sourceFile)
    {
        return base.IsApplicable(sourceFile) && sourceFile.LanguageType.Name == "XML";
    }

    [CanBeNull]
    public ITreeNode GetTagByDef(string defType, string defName)
    {
        return GetTagByDef($"{defType}/{defName}");
    }

    [CanBeNull]
    public ITreeNode GetTagByDef(string defId)
    {
        if (!DefTags.ContainsKey(defId))
            return null;

        return DefTags[defId];
    }

    public override object Build(IPsiSourceFile sourceFile, bool isStartup)
    {
        if (!IsApplicable(sourceFile))
            return null;

        if (sourceFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) return null;

        var tags = xmlFile.GetNestedTags<IXmlTag>("Defs/*").Where(tag =>
        {
            var defNameTag = tag.GetNestedTags<IXmlTag>("defName").FirstOrDefault();
            return defNameTag is not null;
        });

        List<RimworldXmlDefSymbol> defs = new();

        foreach (var tag in tags)
        {
            var defName = tag.GetNestedTags<IXmlTag>("defName").FirstOrDefault()?.InnerText;
            var defNameTag = tag.GetNestedTags<IXmlTag>("defName").FirstOrDefault().Children().ElementAt(1);
            if (defName is null) continue;

            defs.Add(new RimworldXmlDefSymbol(defNameTag, defName, tag.GetTagName()));
        }

        return defs;
    }

    public override void Merge(IPsiSourceFile sourceFile, object builtPart)
    {
        RemoveFromLocalCache(sourceFile);
        AddToLocalCache(sourceFile, builtPart as List<RimworldXmlDefSymbol>);
        base.Merge(sourceFile, builtPart);
    }

    public override void MergeLoaded(object data)
    {
        PopulateLocalCache();
        base.MergeLoaded(data);
    }

    public override void Drop(IPsiSourceFile sourceFile)
    {
        RemoveFromLocalCache(sourceFile);
        base.Drop(sourceFile);
    }

    private void AddToLocalCache(IPsiSourceFile sourceFile, [CanBeNull] List<RimworldXmlDefSymbol> cacheItem)
    {
        if (sourceFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) return;

        cacheItem?.ForEach(item =>
        {
            var matchingDefTag = xmlFile
                .GetNestedTags<IXmlTag>("Defs/*/defName").FirstOrDefault(tag =>
                    tag.Children().ElementAt(1).GetTreeStartOffset().Offset == item.DocumentOffset);

            if (matchingDefTag is null) return;
            
            var xmlTag = matchingDefTag.Children().ElementAt(1);
            
            if (!DefTags.ContainsKey($"{item.DefType}/{item.DefName}"))
                DefTags.Add($"{item.DefType}/{item.DefName}", xmlTag);
            else
                DefTags[$"{item.DefType}/{item.DefName}"] = xmlTag;
        });
    }

    private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
    {
        var items = Map!.GetValueSafe(sourceFile);

        items?.ForEach(item =>
        {
            if (DefTags.ContainsKey($"{item.DefType}/{item.DefName}"))
                DefTags.Remove($"{item.DefType}/{item.DefName}");
        });
    }

    private void PopulateLocalCache()
    {
        foreach (var (sourceFile, cacheItem) in Map)
            AddToLocalCache(sourceFile, cacheItem);
    }
}