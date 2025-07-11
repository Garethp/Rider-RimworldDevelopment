using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.ReSharper.Psi.Impl.reflection2.elements.Compiled;
using JetBrains.ReSharper.Psi.Impl.Types;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.VB.Util;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using ReSharperPlugin.RimworldDev.SymbolScope;
using ReSharperPlugin.RimworldDev.TypeDeclaration;

namespace ReSharperPlugin.RimworldDev;

[Language(typeof(XmlLanguage))]
public class RimworldXMLItemProvider : ItemsProviderOfSpecificContext<RimworldXmlCodeCompletionContext>
{
    private static RimworldCSharpLookupFactory LookupFactory = new();

    /**
     * Defines whether this item provider is available in a given context. At the moment, it only provides lookups for
     * XML where it's a tag and you're working on the tag name, so `<{CARET_HERE}>`, but not
     * `<li Class="{CARET_HERE}">`. We'll probably want to expand this a bit later to provide for text auto-completion
     * or for attribute auto completion, unless we make those separate ItemProviders (which might be a good idea).
     */
    protected override bool IsAvailable(RimworldXmlCodeCompletionContext context)
    {
        if ((context.TreeNode is XmlIdentifier identifier && identifier.Parent is XmlTagHeaderNode) ||
            context.TreeNode is XmlTagStartToken) return true;
        if (context.TreeNode is XmlFloatingTextToken && context.TreeNode.NodeType.ToString() == "TEXT") return true;

        if (context.TreeNode is XmlTagEndToken && context.TreeNode.PrevSibling is XmlIdentifier &&
            context.TreeNode.PrevSibling.PrevSibling?.GetText() != "</") return true;

        if (context.TreeNode is XmlValueToken &&
            context.TreeNode.Parent is XmlAttribute attribute &&
            attribute.AttributeName == "ParentName"
           ) return true;

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
        if (!ScopeHelper.UpdateScopes(context.TreeNode.GetSolution())) return false;

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

        if (context.TreeNode is XmlValueToken &&
            context.TreeNode.Parent is XmlAttribute attribute &&
            attribute.AttributeName == "ParentName"
           )
        {
            AddParentNameItems(context, collector);
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

    protected void AddParentNameItems(RimworldXmlCodeCompletionContext context, IItemsCollector collector)
    {
        if (context.TreeNode?.Parent is not XmlAttribute attribute) return;
        if (attribute.Parent is not XmlTagHeaderNode defTag) return;

        var defClassName = defTag.ContainerName;
        var defClass = ScopeHelper.GetScopeForClass(defClassName);
        
        var xmlSymbolTable = context.TreeNode!.GetSolution().GetSolution().GetComponent<RimworldSymbolScope>();

        var keys = xmlSymbolTable.GetDefsByType(defClassName);

        foreach (var key in keys)
        {
            if (!xmlSymbolTable.IsDefAbstract(key)) continue;
            
            var defType = key.Split('/').First();
            var defName = key.Split('/').Last();

            var item = xmlSymbolTable.GetTagByDef(defType, defName);

            var lookup = LookupFactory.CreateDeclaredElementLookupItem(context, defName,
                new DeclaredElementInstance(new XMLTagDeclaredElement(item, defType, defName, false)));
            collector.Add(lookup);
        }
    }

    protected void AddTextLookupItems(RimworldXmlCodeCompletionContext context, IItemsCollector collector)
    {
        var hierarchy = GetHierarchy(context.TreeNode);

        if (hierarchy.Count == 0) return;

        var classContext = GetContextFromHierachy(hierarchy, ScopeHelper.RimworldScope, ScopeHelper.AllScopes);
        if (classContext == null) return;

        if (classContext.GetClrName().FullName == "System.Boolean")
        {
            collector.Add(CSharpLookupItemFactory.Instance.CreateTextLookupItem(context.Ranges, "true", true));
            collector.Add(CSharpLookupItemFactory.Instance.CreateTextLookupItem(context.Ranges, "false", true));

            return;
        }

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

        if (!classContext.GetAllSuperClasses().Any(superClass => superClass.GetClrName().FullName == "Verse.Def") &&
            !classContext.GetAllSuperTypes().Any(superType => superType.GetClrName().FullName == "Verse.Def"))
            return;

        var className = classContext.ShortName;

        var xmlSymbolTable = context.TreeNode!.GetSolution().GetSolution().GetComponent<RimworldSymbolScope>();

        var keys = xmlSymbolTable.GetDefsByType(className);

        foreach (var key in keys)
        {
            var defType = key.Split('/').First();
            var defName = key.Split('/').Last();

            var item = xmlSymbolTable.GetTagByDef(defType, defName);

            var lookup = LookupFactory.CreateDeclaredElementLookupItem(context, defName,
                new DeclaredElementInstance(new XMLTagDeclaredElement(item, defType, defName, false)));
            collector.Add(lookup);
        }
    }

    protected void AddThingDefClasses(RimworldXmlCodeCompletionContext context, IItemsCollector collector,
        ISymbolScope symbolScope, IPsiModule module)
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
                if (currentNode.FirstChild.Children().ElementAt(1).GetText() == "li" && currentNode.FirstChild
                        .Children().FirstOrDefault(child =>
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
        return desiredClass.GetAllClassMembers<IField>()
            .Where(field => !field.Member.GetAttributeInstances(AttributesSource.All)
                .Select(attribute => attribute.GetAttributeShortName()).Contains("UnsavedAttribute"))
            .Where(member =>
            {
                if (member.Member.AccessibilityDomain.DomainType ==
                    AccessibilityDomain.AccessibilityDomainType.PUBLIC) return true;

                var loadAliasAttributes = member
                    .Member
                    .GetAttributeInstances(AttributesSource.All)
                    .FirstOrDefault(attribute => attribute.GetClrName().FullName == "Verse.LoadAliasAttribute");

                if (loadAliasAttributes != null) return true;

                return false;
            })
            .Select(member => member.Member)
            .ToList();
    }

    // Grabs the fields that we can use for a class by looking at that classes fields as well as the fields for all the
    // classes that it inherits from
    public static List<IField> GetAllFields(ITypeElement desiredClass, ISymbolScope symbolScope)
    {
        return desiredClass.GetAllClassMembers<IField>()
            .Where(field => !field.Member.GetAttributeInstances(AttributesSource.All)
                .Select(attribute => attribute.GetAttributeShortName()).Contains("UnsavedAttribute"))
            .Select(member => member.Member)
            .ToList();
    }

    /**
     * This is where a lot of our heavy lifting is going to be. We're taking the a list of strings as a hierarchy
     * (See the docblock for GetHierarchy) and getting the Class for that last item in that list. So for example, if we
     * take a look at `['ThingDef,' 'building', 'fixedStorageSettings', 'filter']`, we'd be looking into
     * `ThingDef.building`, which is a `BuildingProperties` class, so we look into `BuildingProperties.fixedStorageSettings`,
     * which is a `StorageSettings` class which has `StorageSettings.filter` as a `ThingFilter` class, so we return the
     * `ThingFilter` class as a result of this method
     */
    public static ITypeElement GetContextFromHierachy(List<string> hierarchy, ISymbolScope symbolScope,
        List<ISymbolScope> allSymbolScopes)
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
                if (previousField == null)
                {
                    return null;
                }

                string classValue = null;
                if (previousField.Type is ISimplifiedIdTypeInfo simpleTypeInfo)
                {
                    classValue = simpleTypeInfo.GetTypeArguments()?.FirstOrDefault()?
                        .GetLongPresentableName(CSharpLanguage.Instance);
                }

                if (classValue == null)
                {
                    if (!Regex.Match(
                            previousField.Type.GetLongPresentableName(CSharpLanguage.Instance),
                            @"^System.Collections.Generic.List<.*?>$"
                        ).Success)
                    {
                        return null;
                    }

                    // Use regex to grab the className and then fetch it from the symbol scope
                    classValue = Regex.Match(previousField.Type.GetLongPresentableName(CSharpLanguage.Instance),
                        @"^System.Collections.Generic.List<(.*?)>$").Groups[1].Value;
                }

                ;

                currentContext = ScopeHelper.GetScopeForClass(classValue).GetTypeElementByCLRName(classValue);
                continue;
            }

            // Taking a look at `GetHierarchy`, there are instances where we have `<li Class="">`, so we want to pull
            // the class name from that attribute instead of the previous field.
            if (Regex.Match(currentNode, @"^li<(.*?)>$").Success)
            {
                var match = Regex.Match(currentNode, @"^li<(.*?)>$");
                var classValue = match.Groups[1].Value;

                if (classValue == "") return null;

                var scopeToUse = ScopeHelper.GetScopeForClass(classValue);
                // First we try to look it up as a short name from the Rimworld DLL
                currentContext = scopeToUse.GetElementsByShortName(classValue).FirstOrDefault() as ITypeElement;
                if (currentContext != null) continue;

                // Then we try to look it up as a fully qualified name from Rimworld
                currentContext = scopeToUse.GetTypeElementByCLRName(classValue);
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
            if (!currentContext.IsClass())
            {
                currentContext = currentNode.Contains(".")
                    ? ScopeHelper.GetScopeForClass(currentNode)?.GetTypeElementByCLRName(currentNode)
                    : symbolScope?.GetElementsByShortName(currentNode).FirstOrDefault() as Class;
                if (currentContext is null) return null;

                continue;
            }

            var fields = GetAllFields(currentContext, symbolScope);
            var field = fields.FirstOrDefault(
                field => field.ShortName == currentNode
            ) ?? fields.FirstOrDefault(field =>
                field.GetAttributeInstances(AttributesSource.Self).Any(
                    attribute =>
                    {
                        if (attribute.GetClrName().FullName != "Verse.LoadAliasAttribute") return false;
                        if (attribute.PositionParameterCount != 1) return false;

                        var test = field.GetXMLDoc(true);

                        if (!attribute.PositionParameter(0).ConstantValue.IsString())
                        {
                            var writer = new StringWriter(new StringBuilder());
                            if (attribute is not MetadataAttributeInstance) return false;

                            ((MetadataAttributeInstance)attribute).Dump(writer, "");
                            writer.Close();

                            if (writer.ToString().Contains($"Arguments: \"{currentNode}\"")) return true;
                            return false;
                        }

                        return attribute.PositionParameter(0).ConstantValue.StringValue == currentNode;
                    }));

            if (field == null) return null;
            previousField = field;
            var clrName = field.Type.GetLongPresentableName(CSharpLanguage.Instance);

            currentContext = ScopeHelper.GetScopeForClass(clrName).GetTypeElementByCLRName(clrName);

            switch (clrName)
            {
                case "bool":
                    currentContext = symbolScope.GetTypeElementByCLRName("System.Boolean");
                    break;
                case "string":
                    currentContext = symbolScope.GetTypeElementByCLRName("System.String");
                    break;
                case "int":
                    currentContext = symbolScope.GetTypeElementByCLRName("System.Int32");
                    break;
            }
        }

        return currentContext;
    }

    protected ITypeElement GetCurrentClass(RimworldXmlCodeCompletionContext context, ISymbolScope symbolScope,
        List<ISymbolScope> allSymbolScopes)
    {
        var hierarchy = GetHierarchy(context.TreeNode);
        var currentClass = GetContextFromHierachy(hierarchy, symbolScope, allSymbolScopes);
        return currentClass;
    }

    protected void AddProperties(RimworldXmlCodeCompletionContext context, IItemsCollector collector,
        ISymbolScope symbolScope, IPsiModule module)
    {
        var allSymbolScopes = module.GetSolution().PsiModules().GetModules().Select(module =>
            module.GetPsiServices().Symbols.GetSymbolScope(module, true, true)).ToList();

        var parentClass = GetCurrentClass(context, symbolScope, allSymbolScopes);
        if (parentClass is null) return;

        var fields = GetAllPublicFields(parentClass, symbolScope);

        foreach (var field in fields)
        {
            if (!field.IsField || field.AccessibilityDomain.DomainType !=
                AccessibilityDomain.AccessibilityDomainType.PUBLIC)
            {
                var loadAliasAttribute = field.GetAttributeInstances(AttributesSource.All).FirstOrDefault(
                    attribute => attribute.GetClrName().FullName == "Verse.LoadAliasAttribute" &&
                                 attribute.PositionParameterCount == 1 &&
                                 attribute is MetadataAttributeInstance
                );

                if (loadAliasAttribute == null) continue;

                var writer = new StringWriter(new StringBuilder());
                ((MetadataAttributeInstance)loadAliasAttribute).Dump(writer, "");
                writer.Close();

                var match = Regex.Match(writer.ToString(), "Arguments: \"(.*?)\"");
                if (match.Groups.Count < 2) continue;


                collector.Add(LookupFactory.CreateDeclaredElementLookupItem(context, match.Groups[1].Value,
                    new DeclaredElementInstance(field), true, false, QualifierKind.NONE));

                continue;
            }

            var lookup = LookupFactory.CreateDeclaredElementLookupItem(context, field.ShortName,
                new DeclaredElementInstance(field), true, false, QualifierKind.NONE);
            collector.Add(lookup);
        }
    }
}