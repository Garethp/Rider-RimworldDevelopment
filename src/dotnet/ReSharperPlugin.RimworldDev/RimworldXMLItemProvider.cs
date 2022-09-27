using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.CSharp;
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
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;

namespace ReSharperPlugin.RimworldDev;

[Language(typeof(XmlLanguage))]
public class RimworldXMLItemProvider: ItemsProviderOfSpecificContext<RimworldXmlCodeCompletionContext>
{
    private static RimworldCSharpLookupFactory LookupFactory = new ();
    
    protected override bool IsAvailable(RimworldXmlCodeCompletionContext context)
    {
        if ((context.TreeNode is XmlIdentifier identifier && identifier.Parent is XmlTagHeaderNode) || context.TreeNode is XmlTagStartToken) return true;
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

        var rimWorldModule = solution.PsiModules().GetModules()
            .First(assembly => assembly.DisplayName == "Assembly-CSharp");

        var rimworldSymbolScope = rimWorldModule.GetPsiServices().Symbols.GetSymbolScope(rimWorldModule, true, true);

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
                // Let's start checking for Class=""

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

        if (list.ElementAt(0).Equals("Defs"))
        {
            list.RemoveAt(0);
        }

        return list;
    }

    public static List<IField> GetAllPublicFields(ITypeElement desiredClass, ISymbolScope symbolScope)
    {
        // var fields = desiredClass.Fields.ToList();
        var fields = new Dictionary<string, IField>();
        
        desiredClass.Fields.ForEach(field =>
        {
            // if (field.AccessibilityDomain.DomainType != AccessibilityDomain.AccessibilityDomainType.PUBLIC) return;
            
            fields.Add(field.ShortName, field);
        });
        
        var currentClass = desiredClass;
        
        desiredClass.GetSuperTypeElements().ForEach(superType =>
        {
            if (superType.ShortName == "Object") return;

            foreach (var classField in superType.Fields)
            {
                // if (classField.AccessibilityDomain.DomainType != AccessibilityDomain.AccessibilityDomainType.PUBLIC) return;

                if (!fields.ContainsKey(classField.ShortName))
                {
                    fields.Add(classField.ShortName, classField);
                }
            }
        });
        
        // var extendedClassName = desiredClass.ExtendsListNames.FirstOrDefault();
        // if (extendedClassName is null)
        // {
        //     return new List<IField>();
        // }
        //
        // currentClass = symbolScope.GetElementsByShortName(extendedClassName).FirstOrDefault() as Class;
        //
        // while (currentClass is not null && currentClass.ShortName != "Object")
        // {
        //     foreach (var classField in currentClass.Fields)
        //     {
        //         if (!fields.ContainsKey(classField.ShortName))
        //         {
        //             fields.Add(classField.ShortName, classField);
        //         }
        //     }
        //     
        //     var superClasses = currentClass.GetAllSuperClasses();
        //     
        //     var nextExtendedClassName = currentClass.ExtendsListNames.FirstOrDefault();
        //     if (nextExtendedClassName is null) return fields.Values.ToList();
        //     currentClass = symbolScope.GetElementsByShortName(nextExtendedClassName).FirstOrDefault() as Class;
        // }
        //
        return fields.Values.ToList();
    }

    public static ITypeElement GetContextFromHierachy(List<string> hierarchy, ISymbolScope symbolScope, List<ISymbolScope> allSymbolScopes)
    {
        ITypeElement currentContext = null;
        var isList = false;
        IField previousField = null;

        while (hierarchy.Count > 0)
        {
            var currentNode = hierarchy.ElementAt(0);
            hierarchy.RemoveAt(0);

            if (currentNode == "li")
            {
                if (previousField == null ||
                    !Regex.Match(
                        previousField.Type.GetLongPresentableName(CSharpLanguage.Instance),
                        @"^System.Collections.Generic.List<.*?>$"
                    ).Success)
                {
                    return null;
                }
                
                var classValue = Regex.Match(previousField.Type.GetLongPresentableName(CSharpLanguage.Instance),
                    @"^System.Collections.Generic.List<(.*?)>$").Groups[1].Value;

                currentContext = symbolScope.GetTypeElementByCLRName(classValue);
                continue;
            }
            
            if (Regex.Match(currentNode, @"^li").Success)
            {
                var match = Regex.Match(currentNode, @"^li<(.*?)>$");
                var classValue = match.Groups[1].Value;

                currentContext = symbolScope.GetElementsByShortName(classValue).FirstOrDefault() as ITypeElement;
                if (currentContext != null) continue;

                currentContext = symbolScope.GetTypeElementByCLRName(classValue);
                if (currentContext != null) continue;

                // var foundSymbolScope =
                    // allSymbolScopes.FirstOrDefault(scope => scope.GetElementsByQualifiedName(classValue) != null);

                var foundSymbolScope =
                    allSymbolScopes.FirstOrDefault(scope => scope.GetElementsByQualifiedName(classValue).Count > 0);

                if (foundSymbolScope == null) return null;

                var foundClass = foundSymbolScope.GetElementsByQualifiedName(classValue).First();

                currentContext = foundClass as ITypeElement;
                continue;
            }

            // JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2.Class;

            if (currentContext is not Class)
            {
                currentContext = symbolScope.GetElementsByShortName(currentNode).FirstOrDefault() as Class;
                continue;
            }

            var fields = GetAllPublicFields(currentContext, symbolScope);
            var field = fields.First(field => field.ShortName == currentNode);
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