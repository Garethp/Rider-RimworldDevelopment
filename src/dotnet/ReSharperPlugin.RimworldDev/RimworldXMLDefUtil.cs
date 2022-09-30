using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.TestRunner.Abstractions.Extensions;

namespace ReSharperPlugin.RimworldDev;

public class RimworldXMLDefUtil
{
    public static Dictionary<string, IXmlTag> DefTags = new();

    public static void UpdateDefs(ISolution solution)
    {
        var mySolution = solution.GetAllProjects().FirstOrDefault(project => project.Name == "AshAndDust");
        if (mySolution is null) return;
        
        var files = mySolution.GetAllProjectFiles(file => file.LanguageType.Name == "XML");
        
        foreach (var projectFile in files)
        {
            if (projectFile.GetPrimaryPsiFile() is not IXmlFile xmlFile) continue;
            
            xmlFile.GetNestedTags<IXmlTag>("Defs/*").Where(tag =>
            {
                var defNameTag = tag.GetNestedTags<IXmlTag>("defName").FirstOrDefault();
                if (defNameTag is null) return false;

                return true;
            }).ForEach(tag =>
            {
                var defName = tag.GetNestedTags<IXmlTag>("defName").FirstOrDefault()?.InnerText;
                if (defName is null || DefTags.ContainsKey($"{tag.GetTagName()}/{defName}")) return;
                
                DefTags.Add($"{tag.GetTagName()}/{defName}", tag);
            });
        }
        
        return;
    }
}