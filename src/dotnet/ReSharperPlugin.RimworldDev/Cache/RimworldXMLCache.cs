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
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace ReSharperPlugin.RimworldDev.Cache;

[PsiComponent]
public class RimworldXMLCache : SimpleICache<List<RimworldXMLCacheItem>>
{
    public Dictionary<string, IXmlTag> DefTags = new();
    
    public RimworldXMLCache
        (Lifetime lifetime, [NotNull] IShellLocks locks, [NotNull] IPersistentIndexManager persistentIndexManager, long? version = null) 
        : base(lifetime, locks, persistentIndexManager, RimworldXMLCacheItem.Marshaller, version)
    {
    }

    protected override bool IsApplicable(IPsiSourceFile sourceFile)
    {
        return base.IsApplicable(sourceFile) && sourceFile.LanguageType.Name == "XML";
    }

    [CanBeNull]
    public IXmlTag GetTagByDef(string defType, string defName)
    {
        return GetTagByDef($"{defType}/{defName}");
    }
    
    [CanBeNull]
    public IXmlTag GetTagByDef(string defId)
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

        List<RimworldXMLCacheItem> defs = new();

        foreach (var tag in tags)
        {
            var defName = tag.GetNestedTags<IXmlTag>("defName").FirstOrDefault()?.InnerText;
            if (defName is null) continue;

            defs.Add(new RimworldXMLCacheItem(tag, defName, tag.GetTagName()));
        }

        return defs;
    }

    public override void Merge(IPsiSourceFile sourceFile, object builtPart)
    {
        RemoveFromLocalCache(sourceFile);
        AddToLocalCache(sourceFile, builtPart as List<RimworldXMLCacheItem>);
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
    
    private void AddToLocalCache(IPsiSourceFile sourceFile, [CanBeNull] List<RimworldXMLCacheItem> cacheItem)
    {
        if (sourceFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) return;
        
        cacheItem?.ForEach(item =>
        {
            if (!DefTags.ContainsKey($"{item.DefType}/{item.DefName}"))
                DefTags.Add($"{item.DefType}/{item.DefName}", xmlFile.GetNestedTags<IXmlTag>("Defs/*").FirstOrDefault(tag => tag.GetTreeStartOffset().Offset == item.DocumentOffset));
        });
    }

    private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
    {
        var items = Map!.GetValueSafe(sourceFile);
        
        items?.ForEach(item =>
        {
            if (!DefTags.ContainsKey($"{item.DefType}/{item.DefName}"))
                DefTags.Remove($"{item.DefType}/{item.DefName}");
        });
    }
    
    private void PopulateLocalCache()
    {
        foreach (var (sourceFile, cacheItem) in Map)
            AddToLocalCache(sourceFile, cacheItem);
    }
}