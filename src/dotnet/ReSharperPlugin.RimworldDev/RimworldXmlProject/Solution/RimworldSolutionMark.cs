using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.Util;
using ReSharperPlugin.RimworldDev.RimworldXmlProject.Project;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject.Solution;

public class RimworldSolutionMark : SolutionMark
{
    public RimworldSolutionMark(
        [NotNull] VirtualFileSystemPath aboutFile,
        [NotNull] VirtualFileSystemPath solutionFilePath,
        [NotNull] IImmutableEnumerable<ISolutionConfigurationDefaults> solutionConfigurationDefaultsSet
        ) : base(
        solutionFilePath, solutionConfigurationDefaultsSet)
    {
        AboutFile = aboutFile;
    }

    private VirtualFileSystemPath AboutFile;

    public override SolutionStructureChange Update(SolutionMarkUpdateRequest request)
    {
        var change = base.Update(request);

        var rimworldMark = new RimworldProjectMark(this, AboutFile, this);
        
        change.AddedProjects.Add(rimworldMark);
        change.AddedProjects.AddRange(rimworldMark.Dependencies);
        
        return change;
    }
}