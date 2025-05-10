using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.SolutionStructure.SolutionConfigurations;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject;

public class RimworldProjectMark : ProjectMarkBase
{
    private string _name;
    
    public RimworldProjectMark(ISolutionMark solutionMark, [CanBeNull] VirtualFileSystemPath location) :
        base(solutionMark)
    {
        Location = location ?? solutionMark.Location;
        
        var aboutDocument = ScopeHelper.GetXmlDocument(Location.FullPath);
        var modName = aboutDocument?.GetElementsByTagName("ModMetaData")[0]?.GetChildElements("name").FirstOrDefault()?.InnerText;
        var modId = aboutDocument?.GetElementsByTagName("ModMetaData")[0]?.GetChildElements("name").FirstOrDefault()?.InnerText;

        _name = modName ?? modId ?? Location.Parent.Parent.Name;
    }
    
    public RimworldProjectMark(ISolutionMark solutionMark, [CanBeNull] VirtualFileSystemPath location, ISolutionMark solution) :
        this(solutionMark, location)
    {
        var aboutDocument = ScopeHelper.GetXmlDocument(Location.FullPath);
        if (aboutDocument == null) return;
        
        var packageIds = aboutDocument.GetElementsByTagName("packageId");
        
        var dependencies = new List<string>() { "Ludeon.RimWorld" };
        
        for (var i = 0; i < packageIds.Count; i++)
        {
            if (packageIds[i].ParentNode?.ParentNode?.Name != "modDependencies" && packageIds[i].ParentNode?.ParentNode?.ParentNode?.Name != "modDependenciesByVersion") continue;
            
            dependencies.Add(packageIds[i].InnerText);
        }

        var mods = ScopeHelper.GetModLocations(solutionMark.Location.FullPath, dependencies);

        foreach (var dependency in mods.Values)
        {
            var dependencyMark = new RimworldProjectMark(solution,
                VirtualFileSystemPath.TryParse(dependency, InteractionContext.SolutionContext));
            dependencyMark.UpdateParent(this);
            Dependencies.Add(dependencyMark);
        }
    }
    
    public readonly List<RimworldProjectMark> Dependencies = new ();

    public override IProjectConfigurationAndPlatform ActiveConfigurationAndPlatform { get; }
    public override bool IsSolutionFolder => false;
    public override string Name => _name ?? "RimWorld";
    public override VirtualFileSystemPath Location { get; }
    public override Guid Guid => Guid.Parse("{F2A71F9B-5D33-465A-A702-920D77279781}");
    public override Guid TypeGuid => Guid.Parse("{F2A71F9B-5D33-465A-A702-920D77279781}");
}