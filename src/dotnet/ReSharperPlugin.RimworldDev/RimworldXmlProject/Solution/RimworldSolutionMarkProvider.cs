using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.RdBackend.Common.Features.ProjectModel;
using JetBrains.Rider.Model;
using JetBrains.Util;
using ReSharperPlugin.RimworldDev.Settings;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject.Solution;

[ShellComponent]
public sealed class RimworldSolutionMarkProvider : ISolutionMarkProvider
{
    public RimworldSolutionMarkProvider(SettingsAccessor settingsAccessor)
    {
        settingsAccessor.GetSettings();
    }

    public ISolutionMark TryCreate(RdSolutionDescription solutionDescription)
    {
        string solutionDirectory;
        switch (solutionDescription)
        {
            case RdExistingSolution solution:
                solutionDirectory = solution.SolutionDirectory;
                break;
            case RdVirtualSolution virtualSolution:
                solutionDirectory = virtualSolution.SolutionDirectory;
                break;
            default:
                return null;
        }

        var directory = VirtualFileSystemPath.TryParse(solutionDirectory, InteractionContext.SolutionContext);
        var aboutFile = directory.TryCombine("About/About.xml");

        if (!aboutFile.ExistsFile) return null;

        if (solutionDescription is RdExistingSolution)
        {
            var solutionFile = GetSolutionFile(directory);
            if (solutionFile is null) return null;
            
            return new RimworldSolutionMark(aboutFile, solutionFile, new List<ISolutionConfigurationDefaults>());
        }

        return new RimworldVirtualSolutionMark(VirtualFileSystemPath
            .TryParse(solutionDirectory, InteractionContext.SolutionContext).Name, aboutFile);
    }

    [CanBeNull]
    private static VirtualFileSystemPath GetSolutionFile(VirtualFileSystemPath rootDir)
    {
        foreach (var childFile in rootDir.GetChildFiles())
        {
            if (childFile.ExtensionNoDot.Equals("sln", StringComparison.OrdinalIgnoreCase)) return childFile;
        }

        foreach (var childDirectory in rootDir.GetChildDirectories())
        {
            foreach (var childFile in childDirectory.GetChildFiles())
            {
                if (childFile.ExtensionNoDot.Equals("sln", StringComparison.OrdinalIgnoreCase)) return childFile;
            }
        }

        return null;
    }
}