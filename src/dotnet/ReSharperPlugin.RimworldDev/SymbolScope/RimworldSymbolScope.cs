using System.Collections.Generic;
using System.Linq;
using JetBrains;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Application.Threading;
using JetBrains.Collections;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Reader.API;
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

[PsiComponent(Instantiation.ContainerAsyncPrimaryThread)]
public class RimworldSymbolScope : SimpleICache<List<RimworldXmlDefSymbol>>
{
    private Dictionary<string, ITreeNode> DefTags = new();
    private Dictionary<string, string> ExtraDefTagNames = new();
    private Dictionary<string, XMLTagDeclaredElement> _declaredElements = new();
    private SymbolTable _symbolTable;

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

    public bool HasTag(DefNameValue defName) =>
        DefTags.ContainsKey(defName.TagId) || ExtraDefTagNames.ContainsKey(defName.TagId);

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
        if (!DefTags.ContainsKey(defId))
            return null;

        return DefTags[defId];
    }

    public DefNameValue GetDefName(DefNameValue value) =>
        ExtraDefTagNames.TryGetValue(value.TagId, out var defTag) ? new DefNameValue(defTag) : value;

    public List<string> GetDefsByType(string defType)
    {
        return DefTags
            .Keys
            .Where(key => key.StartsWith($"{defType}/"))
            .Select(defId => ExtraDefTagNames.ContainsKey(defId) ? ExtraDefTagNames[defId] : defId)
            .ToList()
            .Concat(
                ExtraDefTagNames.Keys
                    .Where(key => key.StartsWith($"{defType}/"))
                    .Select(key => ExtraDefTagNames[key])
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
        ScopeHelper.UpdateScopes(sourceFile.GetSolution());
        if (sourceFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) return;

        cacheItem?.ForEach(item =>
        {
            var matchingDefTag = xmlFile
                .GetNestedTags<IXmlTag>("Defs/*/defName").FirstOrDefault(tag =>
                    tag.Children().ElementAt(1).GetTreeStartOffset().Offset == item.DocumentOffset);

            if (matchingDefTag is null) return;

            var xmlTag = matchingDefTag.Children().ElementAt(1);

            AddDefTagToList(item, xmlTag);
        });

        void AddDefTagToList(RimworldXmlDefSymbol item, ITreeNode xmlTag)
        {
            using (CompilationContextCookie.GetOrCreate(UniversalModuleReferenceContext.Instance))
            {
                if (item.DefType.Contains(".") && ScopeHelper.RimworldScope is not null)
                {
                    var superClasses = ScopeHelper.GetScopeForClass(item.DefType)?
                        .GetTypeElementByCLRName(item.DefType)?
                        .GetAllSuperClasses().ToList() ?? new();

                    foreach (var superClass in superClasses)
                    {
                        if (superClass.GetClrName().FullName == "Verse.Def") break;

                        var subDefType = superClass.GetClrName().ShortName;
                        if (!ExtraDefTagNames.ContainsKey($"{subDefType}/{item.DefName}"))
                        {
                            ExtraDefTagNames.Add($"{subDefType}/{item.DefName}", $"{item.DefType}/{item.DefName}");
                        }
                        else
                        {
                            ExtraDefTagNames[$"{subDefType}/{item.DefName}"] = $"{item.DefType}/{item.DefName}";
                        }
                    }
                }

                if (!DefTags.ContainsKey($"{item.DefType}/{item.DefName}"))
                    DefTags.Add($"{item.DefType}/{item.DefName}", xmlTag);
                else
                    DefTags[$"{item.DefType}/{item.DefName}"] = xmlTag;
            }
        }
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