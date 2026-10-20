using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.Diagnostic;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.ProjectModel.SolutionStructure.SolutionConfigurations;
using JetBrains.ProjectModel.SolutionStructure.SolutionDefinitions;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject.Solution;

public class RimworldVirtualSolutionMark : ISolutionMark
{
    private readonly IReadOnlyCollection<VirtualFileSystemPath> myProjectFiles;

    public RimworldVirtualSolutionMark([NotNull] string name, [NotNull] VirtualFileSystemPath location,
        [NotNull] IReadOnlyCollection<VirtualFileSystemPath> projectFiles)
    {
        Name = name;
        Location = location;
        myProjectFiles = projectFiles;
        ConfigurationAndPlatformStore = SolutionConfigurationAndPlatformStore.Empty;
    }

    public string Name { get; }

    public VirtualFileSystemPath Location { get; }

    public SolutionFormatVersion GetPlatformVersion() => SolutionFormatVersion.Unknown;

    public IReadOnlyCollection<VirtualFileSystemPath> GetProjectLocations() => myProjectFiles;

    public IReadOnlyCollection<VirtualFileSystemPath> GetSolutionRelatedLocations() =>
        EmptyList<VirtualFileSystemPath>.Instance;

    public ISolutionConfigurationAndPlatform ActiveConfigurationAndPlatform =>
        MissingSolutionConfigurationAndPlatform.Instance;

    public ISolutionConfigurationAndPlatformStore ConfigurationAndPlatformStore { get; }

    public SolutionStructureChange Update(SolutionMarkUpdateRequest request)
    {
        var projectMarks = request.StructureContainer.ProjectMarks;
        var addedProjects = new List<IProjectMark>();
        var updatedProjects = new List<IProjectMark>();

        // See our comment in `RimworldSolutionMarkProvider`, we may be picking up other actual projects that we want
        // to add.
        var existingVirtualMarks = projectMarks.OfType<VirtualProjectMark>().ToList<IProjectMark>();
        foreach (var projectFile in myProjectFiles)
        {
            var existingMark = existingVirtualMarks.FirstOrDefault(mark => mark.Location == projectFile);
            if (existingMark == null)
            {
                addedProjects.Add(new VirtualProjectMark(this, projectFile));
                continue;
            }

            updatedProjects.Add(existingMark);
            existingVirtualMarks.Remove(existingMark);
        }

        var rimworldMarks = projectMarks.OfType<RimworldProjectMark>().ToList();
        if (rimworldMarks.Count == 0)
        {
            var rootProject = new RimworldProjectMark(this, Location, this);
            addedProjects.Add(rootProject);
            addedProjects.AddRange(rootProject.Dependencies);
        }
        else
            updatedProjects.AddRange(rimworldMarks);

        return new SolutionStructureChange(this, addedProjects, existingVirtualMarks, updatedProjects,
            EmptyList<IProjectMark>.InstanceList, EmptyList<SolutionLoadDiagnostic>.InstanceList);
    }

    public IProjectMark AddProject(ProjectDefinitionDescriptor descriptor, IProjectMark parent) =>
        throw new NotImplementedException();

    public IProjectMark RenameProject(IProjectMark project, string name, string path) =>
        throw new NotImplementedException();

    public void RemoveProject(IProjectMark project) => throw new NotImplementedException();

    public void MoveProject(IProjectMark project, IProjectMark parent) => throw new NotImplementedException();
}
