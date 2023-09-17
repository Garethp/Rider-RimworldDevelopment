using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ProjectModel.Impl;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.Diagnostic;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.ProjectModel.SolutionStructure.SolutionConfigurations;
using JetBrains.ProjectModel.SolutionStructure.SolutionDefinitions;
using JetBrains.Util;
using ReSharperPlugin.RimworldDev.RimworldXmlProject.Project;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject.Solution;

public class RimworldVirtualSolutionMark : ISolutionMark
{
    public RimworldVirtualSolutionMark([NotNull] string name, [NotNull] VirtualFileSystemPath location)
    {
        Name = name;
        Location = location;
        ConfigurationAndPlatformStore = SolutionConfigurationAndPlatformStore.Empty;
    }

    public string Name { get; }

    public VirtualFileSystemPath Location { get; }

    public SolutionFormatVersion GetPlatformVersion() => SolutionFormatVersion.Unknown;

    public IReadOnlyCollection<VirtualFileSystemPath> GetProjectLocations() =>
        EmptyList<VirtualFileSystemPath>.Instance;

    public IReadOnlyCollection<VirtualFileSystemPath> GetSolutionRelatedLocations() =>
        EmptyList<VirtualFileSystemPath>.Instance;

    public ISolutionConfigurationAndPlatform ActiveConfigurationAndPlatform =>
        MissingSolutionConfigurationAndPlatform.Instance;

    public ISolutionConfigurationAndPlatformStore ConfigurationAndPlatformStore { get; }

    public SolutionStructureChange Update(SolutionMarkUpdateRequest request)
    {
        var projectMarks = request.StructureContainer.ProjectMarks;
        Assertion.Assert(projectMarks.Count <= 1, "our implementation adds only one project");
        ICollection<IProjectMark> addedProjects = EmptyList<IProjectMark>.Instance;
        ICollection<IProjectMark> updatedProjects = EmptyList<IProjectMark>.Instance;
        if (projectMarks.Count == 0)
        {
            var rootProject = new RimworldProjectMark(this, Location, this);
            addedProjects = new List<IProjectMark>
            {
                rootProject
            };
            
            addedProjects.AddRange(rootProject.Dependencies);
        }
        else
            updatedProjects = new[]
            {
                projectMarks[0]
            };
        
        return new SolutionStructureChange(this, addedProjects, EmptyList<IProjectMark>.Instance, updatedProjects,
            EmptyList<IProjectMark>.InstanceList, EmptyList<SolutionLoadDiagnostic>.InstanceList);
    }

    public IProjectMark AddProject(ProjectDefinitionDescriptor descriptor, IProjectMark parent) =>
        throw new NotImplementedException();

    public IProjectMark RenameProject(IProjectMark project, string name, string path) =>
        throw new NotImplementedException();

    public void RemoveProject(IProjectMark project) => throw new NotImplementedException();

    public void MoveProject(IProjectMark project, IProjectMark parent) => throw new NotImplementedException();
}