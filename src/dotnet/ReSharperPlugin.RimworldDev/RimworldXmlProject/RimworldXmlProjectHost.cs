using System;
using System.Collections.Generic;
using JetBrains.Annotations;
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

[ProjectsHostComponent]
public class RimworldXmlProjectHost : SolutionFileProjectHostBase
{
    private readonly IPlatformManager myPlatformManager;
    private readonly FileSystemStructureBuilder myStructureBuilder;
    private readonly FileSystemWildcardService myWildcardService;
    private readonly ProjectFilePropertiesFactory myProjectFilePropertiesFactory;
    
    public RimworldXmlProjectHost(
        IPlatformManager platformManager,
        ProjectFilePropertiesFactory projectFilePropertiesFactory,
        FileSystemStructureBuilder structureBuilder,
        FileSystemWildcardService wildcardService,
        ISolutionMark solutionMark,
        FileContentTracker fileContentTracker)
        : base(solutionMark, fileContentTracker)
    {
        myProjectFilePropertiesFactory = projectFilePropertiesFactory;
        myPlatformManager = platformManager;
        myStructureBuilder = structureBuilder;
        myWildcardService = wildcardService;
    }

    public override bool IsApplicable(IProjectMark projectMark)
    {
        return projectMark.Guid.ToString() == "f2a71f9b-5d33-465a-a702-920d77279781";
    }

    protected override void Reload(ProjectHostReloadChange change, FileSystemPath logPath)
    {
        var projectMark = change.ProjectMark;
        var siteProjectLocation = GetProjectLocation(projectMark);

        var projectName = projectMark.Location.Parent.Parent.Name;
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
        
        var customDescriptor = new RimworldProjectDescriptor(projectMark.Guid, projectProperties, null, projectName,
            siteProjectLocation, projectMark.Location);

        var byProjectLocation = ProjectDescriptor.CreateWithoutItemsByProjectDescriptor(customDescriptor);
        
        Build(byProjectLocation, ProjectFolderFilter.Instance);
        myWildcardService.RegisterDirectory(projectMark, siteProjectLocation, targetFramework, ProjectFolderFilter.Instance);
        
        change.Descriptors = new ProjectHostChangeDescriptors(byProjectLocation)
        {
            ProjectReferencesDescriptor =
                BuildReferences(targetFramework)
        };
    }

    private void Build([NotNull] ProjectDescriptor descriptor, [CanBeNull] IFolderFilter filter = null)
    {
        if (!descriptor.Location.ExistsDirectory)
            return;

        var realParent = new ProjectFolderDescriptor(descriptor.Location.Parent); 
        BuildInternal(realParent, descriptor.ProjectProperties, filter);
        foreach (var projectItem in realParent.Items)
        {
            descriptor.Items.Add(projectItem);
        }
    }

    private void BuildInternal(
        [NotNull] IProjectFolderDescriptor parent,
        [NotNull] IProjectProperties projectProperties,
        [CanBeNull] IFolderFilter filter)
    {
        foreach (var child in parent.Location.GetChildren())
        {
            var absolutePath = parent.Location.TryCombine(child.ToString());
            if (Filter(absolutePath) || (filter != null && filter.Filter(absolutePath))) continue;
            if (child.IsDirectory)
            {
                var parent1 = new ProjectFolderDescriptor(absolutePath, Array.Empty<IProjectItemDescriptor>());
                parent.Items.Add(parent1);
                BuildInternal(parent1, projectProperties, filter);
            }
            else if (child.IsFile)
            {
                var projectFileProperties = myProjectFilePropertiesFactory.CreateProjectFileProperties(projectProperties);
                var projectFileDescriptor = new ProjectFileDescriptor(null, absolutePath, projectFileProperties);
                parent.Items.Add(projectFileDescriptor);
            }
        }
    }

    private bool Filter(VirtualFileSystemPath path) => FileSystemWellKnownFilter.WellKnownExcludeDirectories.Contains(path.Name) || FileSystemWellKnownFilter.IsWellKnownExcludeFiles(path.Name);


    [NotNull]
    private static VirtualFileSystemPath GetProjectLocation([NotNull] IProjectMark projectMark)
    {
        var location = projectMark.Location.Parent;
        return location;
    }

    private static ProjectReferencesDescriptor BuildReferences([NotNull] TargetFrameworkId targetFrameworkId)
    {
        List<Pair<IProjectReferenceDescriptor, IProjectReferenceProperties>> pairList =
            new List<Pair<IProjectReferenceDescriptor, IProjectReferenceProperties>>();
        
        return !pairList.IsEmpty()
            ? new ProjectReferencesDescriptor(pairList)
            : null;
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
            (!path.FullPath.Contains("About") && !path.FullPath.Contains("Defs"));
    }
}