using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.Util;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace ReSharperPlugin.RimworldDev;

[Language(typeof(XmlLanguage))]
public class RimworldXMLItemProvider: ItemsProviderOfSpecificContext<RimworldXmlCodeCompletionContext>
{
    private static RimworldCSharpLookupFactory LookupFactory = new ();
    
    /**
     * Defines whether this item provider is available in a given context. At the moment, it only provides lookups for
     * XML where it's a tag and you're working on the tag name, so `<{CARET_HERE}>`, but not
     * `<li Class="{CARET_HERE}">`. We'll probably want to expand this a bit later to provide for text auto-completion
     * or for attribute auto completion, unless we make those separate ItemProviders (which might be a good idea).
     */
    protected override bool IsAvailable(RimworldXmlCodeCompletionContext context)
    {
        if ((context.TreeNode is XmlIdentifier identifier && identifier.Parent is XmlTagHeaderNode) || context.TreeNode is XmlTagStartToken) return true;
        if (context.TreeNode is XmlFloatingTextToken && context.TreeNode.NodeType.ToString() == "TEXT") return true;

        if (context.TreeNode is XmlTagEndToken && context.TreeNode.PrevSibling is XmlIdentifier &&
            context.TreeNode.PrevSibling.PrevSibling?.GetText() != "</") return true;
        
        return false;
    }

    private XmlTag GetParentTag(ITreeNode xmlTreeNode)
    {
        ITreeNode currentNode = xmlTreeNode;

        while (currentNode.NodeType.ToString() != "FILE")
        {
            currentNode = currentNode.Parent;
            if (currentNode is XmlTag tagNode) return tagNode;
        }
        
        return null;
    }

    public static string GetTagName(XmlTag tag)
    {
        var identifier = tag.FirstChild.Children().FirstOrDefault(tag => tag is XmlIdentifier);
        if (identifier is XmlIdentifier) return identifier.GetText();

        return null;
    }
    
    protected override bool AddLookupItems(RimworldXmlCodeCompletionContext context, IItemsCollector collector)
    {
        ScopeHelper.UpdateScopes(context.TreeNode.GetSolution());
        
        if (context.TreeNode is XmlFloatingTextToken && context.TreeNode.NodeType.ToString() == "TEXT")
        {
            AddTextLookupItems(context, collector);
            return base.AddLookupItems(context, collector);
        }
        
        if (context.TreeNode is XmlTagEndToken && context.TreeNode.PrevSibling is XmlIdentifier &&
            context.TreeNode.PrevSibling.PrevSibling?.GetText() != "</")
        {
            AddTextLookupItems(context, collector);
            return base.AddLookupItems(context, collector);
        }

        /**
         * <Defs>
         *  <{CARET HERE
         *
         * TreeNode is the identifier that the CARET is currently typing
         * First Parent is the TagHeader node
         * Second Parent is the XML Tag that your caret is in
         * Third parent is the XML Tag above that (<Defs>)
         * Third Parent's First child is the header for that tag (Defs)
         */
        ITreeNode currentTag = GetParentTag(context.TreeNode);
        if (context.TreeNode is XmlTagStartToken)
        {
            currentTag = context.TreeNode;
        }
        
        var parentTag = GetParentTag(currentTag);
        var parentTagName = GetTagName(parentTag);
        
        var solution = context.TreeNode.GetSourceFile().PsiModule.GetSolution();

        // Here we're fetching the CSharp Symbol Scope for Rimworld so that we can do our autocomplete.
        // TODO: Detect based on something else, maybe look for Rimworld's class
        // TODO: Don't crash if there's no scope
        // var rimWorldModule = solution.PsiModules().GetModules()
            // .First(assembly => assembly.DisplayName == "Assembly-CSharp");

        var rimWorldModule = ScopeHelper.RimworldModule;
        
        var rimworldSymbolScope = ScopeHelper.RimworldScope;

        if (parentTagName == "Defs")
        {        
            AddThingDefClasses(context, collector, rimworldSymbolScope, rimWorldModule);
        }
        else
        {
            AddProperties(context, collector, rimworldSymbolScope, rimWorldModule);
        }

        return base.AddLookupItems(context, collector);
    }

    protected void AddTextLookupItems(RimworldXmlCodeCompletionContext context, IItemsCollector collector)
    {
        var element= context.TreeNode;
        var hierarchy = GetHierarchy(element);

        if (hierarchy.Count == 0) return;

        var classContext = GetContextFromHierachy(hierarchy, ScopeHelper.RimworldScope, ScopeHelper.AllScopes);
        if (classContext == null) return;

        if (classContext.GetType().Name == "Struct")
        {
            var options = classContext.Fields.Where(field => field.IsStatic);
            
            foreach (var option in options)
            {
                var lookup = LookupFactory.CreateDeclaredElementLookupItem(
                    context,
                    option.ShortName,
                    new DeclaredElementInstance(option, EmptySubstitution.INSTANCE)
                );
                collector.Add(lookup);
            }

            return;
        }
        
        if (classContext.GetType().Name == "Enum")
        {
            var options = classContext.GetMembers();
            
            foreach (var option in options)
            {
                var lookup = LookupFactory.CreateDeclaredElementLookupItem(
                    context,
                    option.ShortName,
                    new DeclaredElementInstance(option, EmptySubstitution.INSTANCE)
                );
                collector.Add(lookup);
            }

            return;
        }
        
        if (!classContext.GetAllSuperClasses().Any(superClass => superClass.GetClrName().FullName == "Verse.Def"))
            return;

        var className = classContext.ShortName;

        var keys = RimworldXMLDefUtil.DefTags.Keys
            .Where(key => key.StartsWith($"{className}/"))
            .Select(key => key.Substring(className.Length + 1));
        
        keys.ForEach(key =>
        {
            var item = RimworldXMLDefUtil.DefTags[$"{className}/{key}"];
            
            var lookup = LookupFactory.CreateDeclaredElementLookupItem(context, key, new DeclaredElementInstance(new XMLTagDeclaredElement(item, key, false)));
            collector.Add(lookup);
        });
        
        return;
    }
    
    protected void AddThingDefClasses(RimworldXmlCodeCompletionContext context, IItemsCollector collector, ISymbolScope symbolScope, IPsiModule module)
    {
        var defType = symbolScope
            .GetTypeElementByCLRName("Verse.Def");
            
        var consumer = new SearchResultsConsumer();
        var pi = NullProgressIndicator.Create();
        module.GetPsiServices().Finder.FindInheritors(defType, symbolScope, consumer, pi);

        foreach (var occurrence in consumer.GetOccurrences())
        {
            var lookup = LookupFactory.CreateDeclaredElementLookupItem(
                context,
                occurrence.GetDeclaredElement().ShortName,
                new DeclaredElementInstance(occurrence.GetDeclaredElement(), EmptySubstitution.INSTANCE)
            );
            collector.Add(lookup);
        }
    }

    /**
     * This essentially attempts to convert XML from a structure similar to
     * ```
     * <Defs>
     *  <ThingDef>
     *   <building>
     *    <fixedStorageSettings>
     *     <filter>
     * ```
     *
     * Into an array like `['ThingDef,' 'building', 'fixedStorageSettings', 'filter']` so that later we can walk
     * down the array and recursively fetch child props of classes to discover where we are
     */
    public static List<string> GetHierarchy(ITreeNode treeNode)
    {
        var list = new List<string>();
        
        var currentNode = treeNode;
        while (currentNode.NodeType.ToString() != "FILE" && currentNode.NodeType.ToString() != "SANDBOX")
        {
            if (currentNode.NodeType.ToString() != "TAG")
            {
                currentNode = currentNode.Parent;
                continue;
            }

            if (!currentNode.FirstChild.Children().ElementAt(1).Equals(treeNode))
            {
                // If we're in an <li> and there's a Class="" attribute, let's return it as li<Class> so that we can handle it later
                if (currentNode.FirstChild.Children().ElementAt(1).GetText() == "li" && currentNode.FirstChild.Children().FirstOrDefault(child =>
                    {
                        if (child is not XmlAttribute attr) return false;
                        return attr.AttributeName == "Class";
                    }) is XmlAttribute classAttr)
                {
                    list.Insert(0, $"li<{classAttr.Value.UnquotedValue}>");
                }
                else
                {
                    list.Insert(0, currentNode.FirstChild.Children().ElementAt(1).GetText());
                }
            }

            currentNode = currentNode.Parent;
        }

        // We never actually want to autocomplete Defs, remove that. We autocomplete its list of children somewhere else
        if (list.Count > 0 && list.ElementAt(0).Equals("Defs"))
        {
            list.RemoveAt(0);
        }

        return list;
    }

    // Grabs the fields that we can use for a class by looking at that classes fields as well as the fields for all the
    // classes that it inherits from
    public static List<IField> GetAllPublicFields(ITypeElement desiredClass, ISymbolScope symbolScope)
    {
        var fields = new Dictionary<string, IField>();
        
        desiredClass.Fields.ForEach(field =>
        {
            fields.Add(field.ShortName, field);
        });
        
        desiredClass.GetAllSuperClasses().ForEach(superClass =>
        {
            if (superClass.GetClassType() is not Class superType) return;
            if (superClass.GetClrName().FullName == "System.Object") return;
            
            if (superType.ShortName == "Object") return;

            foreach (var classField in superType.Fields)
            {
                if (!fields.ContainsKey(classField.ShortName))
                {
                    fields.Add(classField.ShortName, classField);
                }
            }
        });
        
        return fields.Values.ToList();
    }

    /**
     * This is where a lot of our heavy lifting is going to be. We're taking the a list of strings as a hierarchy
     * (See the docblock for GetHierarchy) and getting the Class for that last item in that list. So for example, if we
     * take a look at `['ThingDef,' 'building', 'fixedStorageSettings', 'filter']`, we'd be looking into
     * `ThingDef.building`, which is a `BuildingProperties` class, so we look into `BuildingProperties.fixedStorageSettings`,
     * which is a `StorageSettings` class which has `StorageSettings.filter` as a `ThingFilter` class, so we return the
     * `ThingFilter` class as a result of this method
     */
    public static ITypeElement GetContextFromHierachy(List<string> hierarchy, ISymbolScope symbolScope, List<ISymbolScope> allSymbolScopes)
    {
        ITypeElement currentContext = null;
        var isList = false;
        IField previousField = null;

        // Walking down the tree
        while (hierarchy.Count > 0)
        {
            var currentNode = hierarchy.ElementAt(0);
            hierarchy.RemoveAt(0);

            // List items need to be handled separately, since they don't actually contain the type we need to be looking
            // into, but rather we need to look at the type properties of the previous field
            if (currentNode == "li")
            {
                // If we know we're in an li but can't pull the expected class from the C# typing for some reason, just
                // return null so that we don't throw an error
                if (previousField == null ||
                    !Regex.Match(
                        previousField.Type.GetLongPresentableName(CSharpLanguage.Instance),
                        @"^System.Collections.Generic.List<.*?>$"
                    ).Success)
                {
                    return null;
                }
                
                // Use regex to grab the className and then fetch it from the symbol scope
                var classValue = Regex.Match(previousField.Type.GetLongPresentableName(CSharpLanguage.Instance),
                    @"^System.Collections.Generic.List<(.*?)>$").Groups[1].Value;

                currentContext = symbolScope.GetTypeElementByCLRName(classValue);
                continue;
            }
            
            // Taking a look at `GetHierarchy`, there are instances where we have `<li Class="">`, so we want to pull
            // the class name from that attribute instead of the previous field.
            if (Regex.Match(currentNode, @"^li<(.*?)>$").Success)
            {
                var match = Regex.Match(currentNode, @"^li<(.*?)>$");
                var classValue = match.Groups[1].Value;
                
                if (classValue == "") return null;
                
                // First we try to look it up as a short name from the Rimworld DLL
                currentContext = symbolScope.GetElementsByShortName(classValue).FirstOrDefault() as ITypeElement;
                if (currentContext != null) continue;

                // Then we try to look it up as a fully qualified name from Rimworld
                currentContext = symbolScope.GetTypeElementByCLRName(classValue);
                if (currentContext != null) continue;

                // If it's not a Rimworld class, let's assume that it's a custom class in our own C#. In that case, let's
                // try to find a symbol scope that this exists in
                var foundSymbolScope =
                    allSymbolScopes.FirstOrDefault(scope => scope.GetElementsByQualifiedName(classValue).Count > 0);

                // If it doesn't exist in any symbol scope, return null so we don't throw an error
                if (foundSymbolScope == null) return null;

                var foundClass = foundSymbolScope.GetElementsByQualifiedName(classValue).First();

                currentContext = foundClass as ITypeElement;
                continue;
            }

            // I believe this is just for handling the first item in the hierarchy array, when we haven't set up a
            // current context to be diving into
            if (currentContext is not Class)
            {
                currentContext = symbolScope.GetElementsByShortName(currentNode).FirstOrDefault() as Class;
                continue;
            }

            var fields = GetAllPublicFields(currentContext, symbolScope);
            var field = fields.FirstOrDefault(field => field.ShortName == currentNode);
            if (field == null) return null;
            previousField = field;
            var clrName = field.Type.GetLongPresentableName(CSharpLanguage.Instance);
            
            currentContext = symbolScope.GetTypeElementByCLRName(clrName);
        }

        return currentContext;
    }

    protected ITypeElement GetCurrentClass(RimworldXmlCodeCompletionContext context, ISymbolScope symbolScope, List<ISymbolScope> allSymbolScopes)
    {
        var hierarchy = GetHierarchy(context.TreeNode);
        var currentClass = GetContextFromHierachy(hierarchy, symbolScope, allSymbolScopes);
        return currentClass;
    }

    protected void AddProperties(RimworldXmlCodeCompletionContext context, IItemsCollector collector, ISymbolScope symbolScope, IPsiModule module)
    {
        var allSymbolScopes = module.GetSolution().PsiModules().GetModules().Select(module =>
            module.GetPsiServices().Symbols.GetSymbolScope(module, true, true)).ToList();
        
        var parentClass = GetCurrentClass(context, symbolScope, allSymbolScopes);
        if (parentClass is null) return;

        var fields = GetAllPublicFields(parentClass, symbolScope);

        foreach (var field in fields)
        {
            if (!field.IsField || field.AccessibilityDomain.DomainType !=
                AccessibilityDomain.AccessibilityDomainType.PUBLIC) continue;

            var lookup = LookupFactory.CreateDeclaredElementLookupItem(context, field.ShortName, new DeclaredElementInstance(field), true, false, QualifierKind.NONE);
            collector.Add(lookup);
        }
    }
}