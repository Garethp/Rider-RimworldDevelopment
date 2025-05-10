using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject;

[SolutionInstanceComponent(Instantiation.DemandAnyThreadSafe)]
public class RimworldProjectMarkProvider : ICustomProjectMarkProvider
{
    private readonly ISolutionMark mySolutionMark;

    public RimworldProjectMarkProvider(ISolutionMark solutionMark)
    {
        mySolutionMark = solutionMark;
    }
  
    public IEnumerable<IProjectMark> GetCustomProjectMarks(IReadOnlyCollection<IProjectMark> projectMarks)
    {
        var directory = mySolutionMark.Location;
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

        if (!aboutFile.ExistsFile) return [];

        var project = new RimworldProjectMark(mySolutionMark, aboutFile, mySolutionMark);
        
        return [
            project,
            .. project.Dependencies
        ];
    }
}