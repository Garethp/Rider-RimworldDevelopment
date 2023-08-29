using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.platforms;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Platforms;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.ProjectModel.ProjectsHost.Impl.FileSystem;
using JetBrains.ProjectModel.ProjectsHost.LiveTracking;
using JetBrains.ProjectModel.Properties;
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

        var projectName = projectMark.Name;
        var targetFramework =
            TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, null, ProfileIdentifier.Default);
        var defaultLanguage = ProjectLanguage.JAVASCRIPT;
        var projectProperties = RimworldProjectPropertiesFactory.CreateProjectProperties(
            new TargetFrameworkId[1]
            {
                targetFramework
            }, defaultLanguage, EmptyList<Guid>.InstanceList);
        
        var projectFileProperties = this.myProjectFilePropertiesFactory.CreateProjectFileProperties(projectProperties);
        var byProjectLocation = ProjectDescriptor.CreateByProjectLocation(projectMark.Guid,projectProperties, null, siteProjectLocation, projectName);
        myStructureBuilder.Build(byProjectLocation, ProjectFolderFilter.Instance);
        myWildcardService.RegisterDirectory(projectMark, siteProjectLocation, targetFramework, ProjectFolderFilter.Instance);
        
        // byProjectLocation.Items.Add(new ProjectFileDescriptor("About/About.xml", projectMark.Location, projectFileProperties));
        
        change.Descriptors = new ProjectHostChangeDescriptors(byProjectLocation)
        {
            ProjectReferencesDescriptor =
                BuildReferences(targetFramework)
        };
        
        return;
    }

    [NotNull]
    private static VirtualFileSystemPath GetProjectLocation([NotNull] IProjectMark projectMark)
    {
        var location = projectMark.Location.Parent.Parent;
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