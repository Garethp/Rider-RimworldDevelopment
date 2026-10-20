using System;
using System.Linq;
using JetBrains.Application;
using JetBrains.ProjectModel.ProjectsHost;
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
        // This check is here to make sure we're only trying to adopt projects that opened up in Folder View, IE: they
        // don't have a .sln file. If they do have a .sln file, we'll use the RimworldProjectMarkProvider instead.
        if (solutionDescription is not RdVirtualSolution virtualSolution) return null;

        var directory = VirtualFileSystemPath.TryParse(virtualSolution.SolutionDirectory.Value, InteractionContext.SolutionContext);
        if (directory.IsEmpty) return null;

        var projectDirectory = directory;
        var aboutFile = projectDirectory.TryCombine("About/About.xml");
        var parentAttempts = 0;
        while (!aboutFile.ExistsFile)
        {
            parentAttempts++;
            projectDirectory = projectDirectory.Parent;
            if (projectDirectory.Exists == FileSystemPath.Existence.Missing || parentAttempts >= 5) break;

            aboutFile = projectDirectory.TryCombine("About/About.xml");
        }

        if (!aboutFile.ExistsFile) return null;

        // When we come in as a folder project we may have other "projects" that we do or don't want to load. De default
        // Rider will pass us a list where it's basically a file for ever folder, since it assumes each folder is its own
        // project. We don't just want to throw them away (maybe someone has a .csproj or an obscure F# project without
        // using a .sln file because reasons) (If you are using F# for your mods, respect), we want to load the ones that
        // are legitimate projects. So we'll filter files down by ending in .*proj and let them get added to our custom
        // virtual solution.
        var projectFiles = virtualSolution.ProjectFilePaths
            .Where(path => path.Value.EndsWith("proj", StringComparison.OrdinalIgnoreCase))
            .Select(path => VirtualFileSystemPath.TryParse(path.Value, InteractionContext.SolutionContext))
            .Where(path => !path.IsEmpty)
            .ToList();

        return new RimworldVirtualSolutionMark(directory.Name, aboutFile, projectFiles);
    }
}
