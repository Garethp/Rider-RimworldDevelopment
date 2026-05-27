using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

public struct TranslationKey
{
    public TranslationKey(string language, string keyName, IXmlTag tag)
    {
        Language = language;
        KeyName = keyName;
        Tag = tag;
    }
    
    public string Language { get; }
    public string KeyName { get; }
    
    public IXmlTag Tag { get; }
}

[PsiComponent(Instantiation.ContainerAsyncPrimaryThread)]
public class RimworldKeyedTranslationSymbolScope: SimpleICache<List<RimworldKeyedTranslationSymbol>>
{
    private Dictionary<string, Dictionary<string, TranslationKey>> keyedTranslations;
    private Dictionary<string, XMLTagDeclaredElement> declaredElements = new();
    private SymbolTable symbolTable;

    private string defaultLanguage = "English";
    private string ideLanguage = "English";
    
    public List<string> GetKeys()
    {
        var keys = new List<string>();
        foreach (var language in keyedTranslations.Values)
        {
            keys.AddRange(language.Keys);
        }
        
        return keys.Distinct().ToList();
    }

    public TranslationKey? GetTranslationKey(string key)
    {
        if (!keyedTranslations.ContainsKey(ideLanguage))
            keyedTranslations[ideLanguage] = new ();

        if (!keyedTranslations[ideLanguage].ContainsKey(key))
            return null;

        return keyedTranslations[ideLanguage][key];
    }

    public List<TranslationKey> GetAllTagsForKey(string key)
    {
        var tags = new List<TranslationKey>();
        
        foreach (var language in keyedTranslations.Values)
        {
            if (!language.ContainsKey(key)) continue;
            tags.Add(language[key]);
        }

        return tags;
    }

    public bool HasTranslationKey(string key)
    {
        if (keyedTranslations[ideLanguage].ContainsKey(key)) return true;
        if (ideLanguage != defaultLanguage && keyedTranslations[defaultLanguage].ContainsKey(key)) return true;

        return keyedTranslations.Any(language => language.Value.ContainsKey(key));
    }
    
    public RimworldKeyedTranslationSymbolScope(
        Lifetime lifetime, 
        [NotNull] IShellLocks locks, 
        [NotNull] IPersistentIndexManager persistentIndexManager, 
        long? version = null
    ) : base(lifetime, locks, persistentIndexManager, RimworldKeyedTranslationSymbol.Marshaller, version)
    {
        keyedTranslations = new()
        {
            { defaultLanguage, new() },
        };

        if (defaultLanguage != ideLanguage)
        {
            keyedTranslations.Add(ideLanguage, new());
        }
    }

    public override object Build(IPsiSourceFile sourceFile, bool isStartup)
    {
        if (!IsApplicable(sourceFile)) return null;
        if (sourceFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) return null;
        if (!sourceFile.DisplayName.Contains("Languages")) return null;

        var languageMatch = Regex.Match(sourceFile.DisplayName, @"Languages\\(.*?)\\Keyed");
        if (!languageMatch.Success) return null;

        var language = languageMatch.Groups[1].Value;
        
        var tags = xmlFile.GetNestedTags<IXmlTag>("LanguageData/*").Where(tag => true);

        var symbols = tags.Select(tag => new RimworldKeyedTranslationSymbol(
            tag,
            tag.GetName().XmlName,
            language
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
            if (keyedTranslations.ContainsKey(item.Langauge) && keyedTranslations[item.Langauge].ContainsKey(item.KeyName))
                keyedTranslations[item.Langauge].Remove(item.KeyName);
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
        if (!keyedTranslations.ContainsKey(item.Langauge))
            keyedTranslations[item.Langauge] = new ();

        var translationKey = new TranslationKey(item.Langauge, item.KeyName, xmlTag);
        
        if (!keyedTranslations[item.Langauge].ContainsKey(item.KeyName))
            keyedTranslations[item.Langauge].Add(item.KeyName, translationKey);
        else
            keyedTranslations[item.Langauge][item.KeyName] = translationKey;
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
        if (symbolTable == null) symbolTable = new SymbolTable(solution.GetPsiServices());

        if (declaredElements.ContainsKey($"{language}/{keyName}"))
        {
            declaredElements[$"{language}/{keyName}"].Update(owner);
            return;
        }

        var declaredElement = new XMLTagDeclaredElement(
            owner,
            $"{language}/{keyName}",
            caseSensitiveName
        );

        // @TODO: We seem to get "Key Already Exists" errors. Race condition?
        declaredElements.Add($"{language}/{keyName}", declaredElement);
        symbolTable.AddSymbol(declaredElement);
    }

    public ISymbolTable GetSymbolTable(ISolution solution)
    {
        if (symbolTable == null) symbolTable = new SymbolTable(solution.GetPsiServices());

        return symbolTable;
    }
}