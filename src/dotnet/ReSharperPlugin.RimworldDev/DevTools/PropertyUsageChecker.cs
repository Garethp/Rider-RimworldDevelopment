// using System.Collections.Generic;
// using System.Linq;
// using JetBrains;
// using JetBrains.Annotations;
// using JetBrains.Application.Threading;
// using JetBrains.Collections;
// using JetBrains.Lifetimes;
// using JetBrains.ReSharper.Psi;
// using JetBrains.ReSharper.Psi.Caches;
// using JetBrains.ReSharper.Psi.Files;
// using JetBrains.ReSharper.Psi.Tree;
// using JetBrains.ReSharper.Psi.Util;
// using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
// using JetBrains.ReSharper.Psi.Xml.Tree;
// using JetBrains.Util;
// using ReSharperPlugin.RimworldDev.SymbolScope;
//
// namespace ReSharperPlugin.RimworldDev.DevTools;
//
// [PsiComponent]
// public class PropertyUsageChecker : SimpleICache<List<PropertyUsageItem>>
// {
//     private static readonly Dictionary<string, int> UsageCount = new();
//
//     public static string GetUsageOutput(int minUsages = 100)
//     {
//         var usages = UsageCount;
//         var itemsToOutput = (from entry in usages orderby entry.Value descending select entry).ToList()
//             .Where(item => item.Value > minUsages)
//             .Select(usage => $"{{\"{usage.Key}\", {usage.Value}}}")
//             .ToList();
//
//         return string.Join(",\r\n", itemsToOutput);
//     }
//
//     public PropertyUsageChecker
//     (Lifetime lifetime, [NotNull] IShellLocks locks, [NotNull] IPersistentIndexManager persistentIndexManager,
//         long? version = null)
//         : base(lifetime, locks, persistentIndexManager, PropertyUsageItem.Marshaller, version)
//     {
//     }
//
//     protected override bool IsApplicable(IPsiSourceFile sourceFile)
//     {
//         return base.IsApplicable(sourceFile) && sourceFile.LanguageType.Name == "XML";
//     }
//
//     public override object Build(IPsiSourceFile sourceFile, bool isStartup)
//     {
//         ScopeHelper.UpdateScopes(sourceFile.GetSolution());
//
//         if (!IsApplicable(sourceFile))
//             return null;
//
//         if (sourceFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) return null;
//
//         List<PropertyUsageItem> usages = new();
//
//         var tags = xmlFile.GetNestedTags<IXmlTag>("Defs/*").Where(tag =>
//         {
//             var defNameTag = tag.GetNestedTags<IXmlTag>("defName").FirstOrDefault();
//             return defNameTag is not null;
//         });
//
//         foreach (var tag in tags)
//         {
//             BuildFromTag(tag, false);
//         }
//
//         return usages;
//     }
//
//     private void BuildFromTag(IXmlTag tag, bool isNested = true)
//     {
//         if (isNested)
//         {
//             var hierachy = RimworldXMLItemProvider.GetHierarchy(tag);
//             var fieldName = hierachy.Pop();
//
//             var context =
//                 RimworldXMLItemProvider.GetContextFromHierachy(hierachy, ScopeHelper.RimworldScope,
//                     ScopeHelper.AllScopes);
//
//
//             if (context is not null)
//             {
//                 var fields = RimworldXMLItemProvider.GetAllPublicFields(context,
//                     ScopeHelper.GetScopeForClass(context.GetClrName().FullName));
//                 var field = fields.FirstOrDefault(field => field.ShortName == fieldName);
//
//                 if (field is not null)
//                 {
//                     var className = field.GetContainingType().GetClrName().FullName;
//                     var fullName = $"{className}::{field.ShortName}";
//
//                     UsageCount.TryAdd(fullName, 0);
//                     UsageCount[fullName]++;
//                 }
//             }
//         }
//
//         var nested = tag.GetNestedTags<IXmlTag>("*");
//
//         foreach (var nestedTag in nested)
//         {
//             BuildFromTag(nestedTag);
//         }
//     }
// }