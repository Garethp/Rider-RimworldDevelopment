using System.Collections.Generic;
using System.Linq;
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
using JetBrains.ReSharper.Psi.Xml.Tree;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace ReSharperPlugin.RimworldDev.SymbolScope;

[PsiComponent(Instantiation.ContainerAsyncPrimaryThread)]
public class RimworldKeyedTranslationSymbolScope: SimpleICache<List<RimworldKeyedTranslationSymbol>>
{
    private Dictionary<string, Dictionary<string, IXmlTag>> KeyedTranslations = new();
    private Dictionary<string, XMLTagDeclaredElement> _declaredElements = new();
    private SymbolTable _symbolTable;

    public List<string> GetKeys()
    {
        if (!KeyedTranslations.ContainsKey("English"))
            KeyedTranslations["English"] = new Dictionary<string, IXmlTag>();

        return KeyedTranslations["English"].Keys.ToList();
    }

    public IXmlTag GetKeyTag(string key)
    {
        if (!KeyedTranslations.ContainsKey("English"))
            KeyedTranslations["English"] = new Dictionary<string, IXmlTag>();

        if (!KeyedTranslations["English"].ContainsKey(key))
            return null;

        return KeyedTranslations["English"][key];
    }
    
    public RimworldKeyedTranslationSymbolScope(
        Lifetime lifetime, 
        [NotNull] IShellLocks locks, 
        [NotNull] IPersistentIndexManager persistentIndexManager, 
        long? version = null
    ) : base(lifetime, locks, persistentIndexManager, RimworldKeyedTranslationSymbol.Marshaller, version)
    {
    }

    public override object Build(IPsiSourceFile sourceFile, bool isStartup)
    {
        if (!IsApplicable(sourceFile)) return null;
        if (sourceFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) return null;

        var tags = xmlFile.GetNestedTags<IXmlTag>("LanguageData/*").Where(tag => true);

        var symbols = tags.Select(tag => new RimworldKeyedTranslationSymbol(
            tag,
            tag.GetName().XmlName,
            "English"
        )).ToList();

        if (symbols.Count == 0) return null;
        return symbols;
    }
    
    public override void Merge(IPsiSourceFile sourceFile, object builtPart)
    {
        RemoveFromLocalCache(sourceFile);
        AddToLocalCache(sourceFile, builtPart as List<RimworldKeyedTranslationSymbol>);
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
    
    private void RemoveFromLocalCache(IPsiSourceFile sourceFile)
    {
        var items = Map!.GetValueSafe(sourceFile);

        items?.ForEach(item =>
        {
            if (KeyedTranslations.ContainsKey(item.Langauge) && KeyedTranslations[item.Langauge].ContainsKey(item.KeyName))
                KeyedTranslations[item.Langauge].Remove(item.KeyName);
        });
    }

    private void PopulateLocalCache()
    {
        foreach (var (sourceFile, cacheItem) in Map)
            AddToLocalCache(sourceFile, cacheItem);
    }

    private void AddToLocalCache(IPsiSourceFile sourceFile, [CanBeNull] List<RimworldKeyedTranslationSymbol> cacheItem)
    {
        if (sourceFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) return;
        
        cacheItem?.ForEach(item =>
        {
            var matchingItem = xmlFile.GetNestedTags<IXmlTag>($"LanguageData/{item.KeyName}").FirstOrDefault();

            if (matchingItem is null) return;

            AddKeyToList(item, matchingItem);
        });
    }

    private void AddKeyToList(RimworldKeyedTranslationSymbol item, IXmlTag xmlTag)
    {
        if (!KeyedTranslations.ContainsKey(item.Langauge))
            KeyedTranslations[item.Langauge] = new Dictionary<string, IXmlTag>();
        
        if (!KeyedTranslations[item.Langauge].ContainsKey(item.KeyName))
            KeyedTranslations[item.Langauge].Add(item.KeyName, xmlTag);
        else
            KeyedTranslations[item.Langauge][item.KeyName] = xmlTag;
    }

    protected override bool IsApplicable(IPsiSourceFile sourceFile)
    {
        return base.IsApplicable(sourceFile) && sourceFile.LanguageType.Name == "XML";
    }
    
    public void AddDeclaredElement(
        ISolution solution, 
        ITreeNode owner, 
        string language, 
        string keyName,
        bool caseSensitiveName)
    {
        if (_symbolTable == null) _symbolTable = new SymbolTable(solution.GetPsiServices());

        if (_declaredElements.ContainsKey($"{language}/{keyName}"))
        {
            _declaredElements[$"{language}/{keyName}"].Update(owner);
            return;
        }

        var declaredElement = new XMLTagDeclaredElement(
            owner,
            $"{language}/{keyName}",
            caseSensitiveName
        );

        // @TODO: We seem to get "Key Already Exists" errors. Race condition?
        _declaredElements.Add($"{language}/{keyName}", declaredElement);
        _symbolTable.AddSymbol(declaredElement);
    }

    public ISymbolTable GetSymbolTable(ISolution solution)
    {
        if (_symbolTable == null) _symbolTable = new SymbolTable(solution.GetPsiServices());

        return _symbolTable;
    }
}