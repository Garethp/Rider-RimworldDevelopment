using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Application.platforms;
using JetBrains.ProjectModel.MSBuild;
using JetBrains.ProjectModel.Platforms;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.ProjectModel.ProjectsHost.Impl.FileSystem;
using JetBrains.ProjectModel.ProjectsHost.LiveTracking;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ProjectModel.Update;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject;

[ProjectsHostComponent(Instantiation.DemandAnyThreadUnsafe)]
public class RimworldXmlProjectHost : SolutionFileProjectHostBase
{
    private readonly IPlatformManager myPlatformManager;
    private readonly RimworldProjectStructureBuilder myStructureBuilder;
    private readonly FileSystemWildcardService myWildcardService;
    private readonly ProjectFilePropertiesFactory myProjectFilePropertiesFactory;
    private bool hasReloaded = false;
    
    public RimworldXmlProjectHost(
        IPlatformManager platformManager,
        ProjectFilePropertiesFactory projectFilePropertiesFactory,
        FileSystemWildcardService wildcardService,
        ISolutionMark solutionMark,
        FileContentTracker fileContentTracker)
        : base(solutionMark, fileContentTracker)
    {
        myProjectFilePropertiesFactory = projectFilePropertiesFactory;
        myPlatformManager = platformManager;
        myStructureBuilder = new RimworldProjectStructureBuilder(myProjectFilePropertiesFactory);
        myWildcardService = wildcardService;
    }
    
    public override bool IsApplicable(IProjectMark projectMark)
    {
        return projectMark.Guid.ToString() == "f2a71f9b-5d33-465a-a702-920d77279781";
    }

    protected override void Reload(ProjectHostReloadChange change, FileSystemPath logPath)
    {
        if (change.ProjectMark is not RimworldProjectMark projectMark) return;

        // This is just a bit of a hacky workaround to re-assign our projects dependencies to be children of our project
        // since it doesn't work with `ICustomProjectMarkProvider` as of Rider 2025.1
        //
        // @TODO: Remove this if that issue gets fixed.
        if (!hasReloaded)
        {
            projectMark.Dependencies.ForEach(dependency => dependency.UpdateParent(projectMark));
            hasReloaded = true;
        }

        var siteProjectLocation = GetProjectLocation(projectMark);

        var targetFramework =
            TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, null, ProfileIdentifier.Default);
        var defaultLanguage = ProjectLanguage.JAVASCRIPT;
        var projectProperties = RimworldProjectPropertiesFactory.CreateProjectProperties(
            new TargetFrameworkId[1]
            {
                targetFramework
            }, defaultLanguage, EmptyList<Guid>.InstanceList);
        
        // This is a quick fix suggested by Jetbrains to fix where Files/Folders get created when adding them to our project
        var config = projectProperties.TryGetConfiguration<IManagedProjectConfiguration>(projectProperties.ActiveConfigurations.TargetFrameworkIds.FirstNotNull());
        config?.UpdatePropertyCollection(x =>
            x[MSBuildProjectUtil.BaseDirectoryProperty] = projectMark.Location.Parent.Parent.FullPath);
        
        var customDescriptor = new RimworldProjectDescriptor(projectMark.Guid, projectProperties, null, projectMark.Name,
            siteProjectLocation, projectMark.Location);

        var byProjectLocation = ProjectDescriptor.CreateWithoutItemsByProjectDescriptor(customDescriptor);
        
        myStructureBuilder.Build(byProjectLocation, ProjectFolderFilter.Instance, GetLoadFolders(projectMark.Location.Parent.Parent));
        myWildcardService.RegisterDirectory(projectMark, siteProjectLocation, targetFramework, ProjectFolderFilter.Instance);
        
        change.Descriptors = new ProjectHostChangeDescriptors(byProjectLocation)
        {
            ProjectReferencesDescriptor = BuildReferences(targetFramework, projectMark)
        };
    }

    private List<string> GetLoadFolders(VirtualFileSystemPath basePath)
    {
        var loadFolders = new List<string>();
        var loadFoldersFile = basePath.TryCombine("LoadFolders.xml");
        if (!loadFoldersFile.ExistsFile) return loadFolders;

        try
        {
            var document = ScopeHelper.GetXmlDocument(loadFoldersFile.FullPath);
            if (document == null) return loadFolders;

            var versionList = new List<string>();
            var versions = document.GetElementsByTagName("loadFolders")[0].ChildNodes;
            for (var i = 0; i < versions.Count; i++)
            {
                versionList.Add(versions[i].Name);
            }

            versionList.Sort();
            
            var folderTags = document.GetElementsByTagName(versionList.Last())[0].ChildNodes;
            for (var i = 0; i < folderTags.Count; i++)
            {
                if (!basePath.TryCombine(folderTags[i].InnerText).ExistsDirectory) continue;

                loadFolders.Add(folderTags[i].InnerText);
            }
        }
        catch (Exception e)
        {
            // ignored
        }

        return loadFolders;
    }

    [NotNull]
    private static VirtualFileSystemPath GetProjectLocation([NotNull] IProjectMark projectMark)
    {
        var location = projectMark.Location.Parent;
        return location;
    }

    private static ProjectReferencesDescriptor BuildReferences([NotNull] TargetFrameworkId targetFrameworkId, [NotNull] IProjectMark projectMark)
    {
        if (projectMark is not RimworldProjectMark rimworldProjectMark || rimworldProjectMark.Dependencies.Count == 0) return null;

        var pairList = new List<Pair<IProjectReferenceDescriptor, IProjectReferenceProperties>>();

        rimworldProjectMark.Dependencies.ForEach(dependency =>
        {
            var reference = new ProjectToProjectReferenceBySearchDescriptor(targetFrameworkId, dependency.ToProjectSearchDescriptor());
            pairList.Add(new Pair<IProjectReferenceDescriptor, IProjectReferenceProperties>(reference, ProjectReferenceProperties.Instance) );
        });

        return new ProjectReferencesDescriptor(pairList);
    }

    private class ProjectFolderFilter : IFolderFilter
    {
        public static readonly IFolderFilter Instance =
            new ProjectFolderFilter();

        private ProjectFolderFilter()
        {
        }

        public bool Filter(VirtualFileSystemPath path) =>
            path.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            path.Name.EndsWith(".DotSettings.user", StringComparison.OrdinalIgnoreCase) ||
            path.Name.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
            !new List<string> {"About", "Defs", "Patches", "Languages", "Sounds", "Textures", "News"}.Any(it => path.FullPath.Contains(it));
    }
}