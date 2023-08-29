using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.ProjectModel.SolutionStructure.SolutionConfigurations;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject;

public class RimworldProjectMark : ProjectMarkBase
{
    public RimworldProjectMark(ISolutionMark solutionMark) : base(solutionMark)
    {
        Location = solutionMark.Location;
    }

    public override IProjectConfigurationAndPlatform ActiveConfigurationAndPlatform { get; }
    public override bool IsSolutionFolder => true;
    public override string Name => "RimWorld";
    public override VirtualFileSystemPath Location { get; }
    public override Guid Guid => System.Guid.Parse("{F2A71F9B-5D33-465A-A702-920D77279781}");
    public override Guid TypeGuid => System.Guid.Parse("{F2A71F9B-5D33-465A-A702-920D77279781}");
}