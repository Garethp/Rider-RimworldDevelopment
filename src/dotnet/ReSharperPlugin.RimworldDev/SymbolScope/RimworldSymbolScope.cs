using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml.Tree;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace ReSharperPlugin.RimworldDev.SymbolScope;

/// <summary>
/// Where a def is declared: the offset of its <c>&lt;defName&gt;</c> text or <c>Name=""</c> value in its file. The tree
/// node itself is only looked up when someone asks for it (<see cref="RimworldSymbolScope.GetTagByDef(string)"/>).
/// </summary>
public readonly struct DefTag
{
    public DefTag(IPsiSourceFile sourceFile, int documentOffset, bool isAbstract)
    {
        SourceFile = sourceFile;
        DocumentOffset = documentOffset;
        IsAbstract = isAbstract;
    }

    public IPsiSourceFile SourceFile { get; }
    public int DocumentOffset { get; }
    public bool IsAbstract { get; }
}

/// <summary>
/// Index of every def in the solution, keyed by <c>"{defType}/{defName}"</c>.
///
/// Merge/MergeLoaded only record where each def lives; they must not touch PSI, because Merge runs in the middle of a
/// document commit, where asking for a PSI file asserts ("Trying to get PSI file for an uncommitted document"). Tree
/// nodes are looked up when queried, and the superclass aliases in <see cref="GetExtraDefTagNames"/> are resolved on the
/// first query after a change, once the RimWorld and mod types can actually be resolved.
/// </summary>
[PsiComponent(Instantiation.ContainerAsyncPrimaryThread)]
public class RimworldSymbolScope : SimpleICache<List<RimworldXmlDefSymbol>>
{
    // Version of the persisted RimworldXmlDefSymbol format; bump it whenever the marshaller changes
    private const long PersistentVersion = 2;

    private readonly ISolution _solution;

    // Written in Merge/Drop (write lock), read by queries (read lock), so the two never overlap
    private readonly Dictionary<string, DefTag> DefTags = new();

    // "ThingDef/CustomThing" -> "MyMod.CustomThingDef/CustomThing". Queries can run on several threads at once, so it's
    // rebuilt under a lock and swapped in whole.
    private Dictionary<string, string> _extraDefTagNames = new();
    private volatile bool _extraDefTagNamesStale;
    private readonly object _extraDefTagNamesLock = new();

    // Offset -> defName/Name value node for each XML file we've looked into. Keyed weakly on the IFile so the nodes
    // go away with the tree instead of being kept alive by the index.
    private readonly ConditionalWeakTable<IXmlFile, Dictionary<int, ITreeNode>> _defNodesByFile = new();

    private Dictionary<string, XMLTagDeclaredElement> _declaredElements = new();
    private SymbolTable _symbolTable;

    public RimworldSymbolScope
    (Lifetime lifetime, [NotNull] IShellLocks locks, [NotNull] IPersistentIndexManager persistentIndexManager,
        ISolution solution)
        : base(lifetime, locks, persistentIndexManager, RimworldXmlDefSymbol.Marshaller, PersistentVersion)
    {
        _solution = solution;
    }

    protected override bool IsApplicable(IPsiSourceFile sourceFile)
    {
        return base.IsApplicable(sourceFile) && sourceFile.LanguageType.Name == "XML";
    }

    /// <summary>
    /// The current aliases, rebuilding them first if they're stale (see <see cref="RebuildExtraDefTagNames"/>). Call it
    /// once per query and use the result throughout: each call may retry the rebuild, and another query may swap in a
    /// new dictionary between calls.
    /// </summary>
    private Dictionary<string, string> GetExtraDefTagNames()
    {
        if (!_extraDefTagNamesStale) return _extraDefTagNames;

        lock (_extraDefTagNamesLock)
        {
            if (_extraDefTagNamesStale) RebuildExtraDefTagNames();
            return _extraDefTagNames;
        }
    }

    public bool HasTag(DefNameValue defName) =>
        DefTags.ContainsKey(defName.TagId) || GetExtraDefTagNames().ContainsKey(defName.TagId);

    [CanBeNull]
    public ITreeNode GetTagByDef(string defType, string defName)
    {
        return GetTagByDef($"{defType}/{defName}");
    }

    [CanBeNull]
    public ITreeNode GetTagByDef(DefNameValue defName) => GetTagByDef(defName.TagId);

    [CanBeNull]
    public ITreeNode GetTagByDef(string defId)
    {
        if (!DefTags.TryGetValue(defId, out var defTag))
            return null;

        return FindDefNode(defTag);
    }

    public bool IsDefAbstract(string defId)
    {
        return DefTags.TryGetValue(defId, out var defTag) && defTag.IsAbstract;
    }

    public DefNameValue GetDefName(DefNameValue value) =>
        GetExtraDefTagNames().TryGetValue(value.TagId, out var defTag) ? new DefNameValue(defTag) : value;

    public List<string> GetDefsByType(string defType)
    {
        var extraDefTagNames = GetExtraDefTagNames();

        return DefTags
            .Keys
            .Where(key => key.StartsWith($"{defType}/"))
            .Select(defId => extraDefTagNames.TryGetValue(defId, out var aliasedDefId) ? aliasedDefId : defId)
            .Concat(
                extraDefTagNames
                    .Where(alias => alias.Key.StartsWith($"{defType}/"))
                    .Select(alias => alias.Value)
            ).ToList();
    }

    public override object Build(IPsiSourceFile sourceFile, bool isStartup)
    {
        if (!IsApplicable(sourceFile))
            return null;

        if (sourceFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) return null;

        var tags = xmlFile.GetNestedTags<IXmlTag>("Defs/*").Where(tag =>
        {
            var defNameTag = tag.GetNestedTags<IXmlTag>("defName").FirstOrDefault();
            if (defNameTag is not null) return true;

            var nameAttribute = tag.GetAttribute("Name");
            return nameAttribute is not null;
        });

        List<RimworldXmlDefSymbol> defs = new();

        foreach (var tag in tags)
        {
            var defName = tag
                              .GetNestedTags<IXmlTag>("defName")
                              .FirstOrDefault()?.InnerText ??
                          tag
                              .GetAttribute("Name")?
                              .Children()
                              .FirstOrDefault(element => element is IXmlValueToken)?
                              .GetUnquotedText();

            var defNameTag = tag.GetNestedTags<IXmlTag>("defName").
                                 FirstOrDefault()?.
                                 Children().
                                 ElementAt(1) ??
                             tag.GetAttribute("Name")?.
                                 Children().
                                 FirstOrDefault(element => element is IXmlValueToken);

            if (defName is null) continue;

            // Only defs identified by a Name="" attribute can be abstract parents
            var isAbstract = defNameTag is IXmlValueToken &&
                             tag.GetAttribute("Abstract") is { } attribute &&
                             attribute.UnquotedValue.ToLower() == "true";

            defs.Add(new RimworldXmlDefSymbol(defNameTag, defName, tag.GetTagName(), isAbstract));
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

    // Runs inside Merge, i.e. mid-commit: must not ask for PSI (see the class comment)
    private void AddToLocalCache(IPsiSourceFile sourceFile, [CanBeNull] List<RimworldXmlDefSymbol> cacheItem)
    {
        cacheItem?.ForEach(item =>
        {
            DefTags[$"{item.DefType}/{item.DefName}"] = new DefTag(sourceFile, item.DocumentOffset, item.IsAbstract);
            if (item.DefType.Contains(".")) _extraDefTagNamesStale = true;
        });
    }

    private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
    {
        var items = Map!.GetValueSafe(sourceFile);

        items?.ForEach(item =>
        {
            var defId = $"{item.DefType}/{item.DefName}";

            if (DefTags.TryGetValue(defId, out var defTag) && defTag.SourceFile.Equals(sourceFile))
                DefTags.Remove(defId);

            if (item.DefType.Contains(".")) _extraDefTagNamesStale = true;
        });
    }

    private void PopulateLocalCache()
    {
        foreach (var (sourceFile, cacheItem) in Map)
            AddToLocalCache(sourceFile, cacheItem);
    }

    /// <summary>
    /// Maps each def whose type is a mod class (<c>&lt;MyMod.CustomThingDef&gt;</c>) to every RimWorld superclass short
    /// name up to <c>Verse.Def</c>, so that a <c>ThingDef</c> reference finds it. Stays stale, so the next query tries
    /// again, until RimWorld's scope is ready and every such type resolves; on a cold load neither is true when the
    /// index is merged.
    /// </summary>
    private void RebuildExtraDefTagNames()
    {
        var customDefs = DefTags.Keys
            .Select(defId => new DefNameValue(defId))
            .Where(defId => defId.DefType.Contains("."))
            .ToList();

        var extraDefTagNames = new Dictionary<string, string>();
        var allResolved = true;

        if (customDefs.Any())
        {
            if (!ScopeHelper.UpdateScopes(_solution)) return;

            foreach (var def in customDefs)
            {
                if (ScopeHelper.GetDefSuperClassNames(def.DefType) is not { } superClassNames)
                {
                    allResolved = false;
                    continue;
                }

                foreach (var superClassName in superClassNames)
                    extraDefTagNames[$"{superClassName}/{def.DefName}"] = def.TagId;
            }
        }

        _extraDefTagNames = extraDefTagNames;
        _extraDefTagNamesStale = !allResolved;
    }

    [CanBeNull]
    private ITreeNode FindDefNode(DefTag defTag)
    {
        var sourceFile = defTag.SourceFile;
        if (!sourceFile.IsValid()) return null;

        // Queries normally run on committed documents, but don't turn a stray one into the assertion this design avoids
        if (!sourceFile.GetPsiServices().Files.IsCommitted(sourceFile)) return null;
        if (sourceFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) return null;

        if (_defNodesByFile.TryGetValue(xmlFile, out var nodes) &&
            nodes.TryGetValue(defTag.DocumentOffset, out var cachedNode) &&
            cachedNode.IsValid() &&
            cachedNode.GetTreeStartOffset().Offset == defTag.DocumentOffset)
            return cachedNode;

        // Not looked at yet, or reparsed since (an incremental reparse can keep the same IFile)
        nodes = FindDefNodes(xmlFile);
        _defNodesByFile.AddOrUpdate(xmlFile, nodes);

        return nodes.TryGetValue(defTag.DocumentOffset, out var node) ? node : null;
    }

    // The same nodes Build records offsets for: the text inside <defName>, and the value of Name=""
    private static Dictionary<int, ITreeNode> FindDefNodes(IXmlFile xmlFile)
    {
        var nodes = new Dictionary<int, ITreeNode>();

        foreach (var tag in xmlFile.GetNestedTags<IXmlTag>("Defs/*"))
        {
            if (tag.GetNestedTags<IXmlTag>("defName").FirstOrDefault()?.Children().ElementAtOrDefault(1) is
                { } defNameValue)
                nodes[defNameValue.GetTreeStartOffset().Offset] = defNameValue;

            if (tag.GetAttribute("Name")?.Children().FirstOrDefault(element => element is IXmlValueToken) is
                { } nameValue)
                nodes[nameValue.GetTreeStartOffset().Offset] = nameValue;
        }

        return nodes;
    }

    public void AddDeclaredElement(ISolution solution, ITreeNode owner, string defType, string defName,
        bool caseSensitiveName)
    {
        if (_symbolTable == null) _symbolTable = new SymbolTable(solution.GetPsiServices());

        if (_declaredElements.ContainsKey($"{defType}/{defName}"))
        {
            _declaredElements[$"{defType}/{defName}"].Update(owner);
            return;
        }

        var declaredElement = new XMLTagDeclaredElement(
            owner,
            defType,
            defName,
            caseSensitiveName
        );

        // @TODO: We seem to get "Key Already Exists" errors. Race condition?
        _declaredElements.Add($"{defType}/{defName}", declaredElement);
        _symbolTable.AddSymbol(declaredElement);
    }

    public ISymbolTable GetSymbolTable(ISolution solution)
    {
        if (_symbolTable == null) _symbolTable = new SymbolTable(solution.GetPsiServices());

        return _symbolTable;
    }
}
