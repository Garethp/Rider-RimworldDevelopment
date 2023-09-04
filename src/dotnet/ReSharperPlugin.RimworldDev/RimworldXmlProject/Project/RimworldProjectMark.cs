using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.ProjectModel.SolutionStructure.SolutionConfigurations;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject.Project;

public class RimworldProjectMark : ProjectMarkBase
{
    public RimworldProjectMark(ISolutionMark solutionMark, [CanBeNull] VirtualFileSystemPath location,
        [CanBeNull] SolutionStructureChange change = null) :
        base(solutionMark)
    {
        Location = location ?? solutionMark.Location;

        if (change != null)
        {
            var dependencies = new List<string>() { "Ludeon.RimWorld" };

            var mods = GetModsList();

            foreach (var dependency in dependencies)
            {
                if (!mods.ContainsKey(dependency)) continue;

                change.AddedProjects.Add(new RimworldProjectMark(change.SolutionMark,
                    VirtualFileSystemPath.TryParse(mods[dependency], InteractionContext.SolutionContext)));
            }
        }

        return;
    }

    public override IProjectConfigurationAndPlatform ActiveConfigurationAndPlatform { get; }
    public override bool IsSolutionFolder => false;
    public override string Name => "RimWorld";
    public override VirtualFileSystemPath Location { get; }
    public override Guid Guid => System.Guid.Parse("{F2A71F9B-5D33-465A-A702-920D77279781}");
    public override Guid TypeGuid => System.Guid.Parse("{F2A71F9B-5D33-465A-A702-920D77279781}");

    private Dictionary<string, string> GetModsList()
    {
        var directoriesToCheck = new System.Collections.Generic.List<string>()
        {
            "D:\\SteamLibrary\\steamapps\\common\\RimWorld\\Mods",
            "D:\\SteamLibrary\\steamapps\\common\\RimWorld\\Data"
        };
        var foundMods = new System.Collections.Generic.Dictionary<string, string>();

        foreach (var directory in directoriesToCheck)
        {
            var path = FileSystemPath.TryParse(directory);
            if (!path.ExistsDirectory) continue;

            foreach (var child in path.GetChildren())
            {
                if (!child.IsDirectory) continue;

                var aboutFile = FileSystemPath.TryParse($@"{directory}\{child.ToString()}\About\About.xml");
                if (!aboutFile.ExistsFile) continue;

                var document = GetXmlDocument(aboutFile.FullPath);
                var modName = document?.GetElementsByTagName("ModMetaData")[0]?.GetChildElements("packageId")
                    .FirstOrDefault()?.InnerText;

                if (modName == null) continue;

                foundMods.Add(modName, aboutFile.FullPath);
            }
        }

        return foundMods;
    }

    [CanBeNull]
    private static XmlDocument GetXmlDocument(string fileLocation)
    {
        if (!File.Exists(fileLocation)) return null;

        using var reader = new StreamReader(fileLocation);
        using var xmlReader = new XmlTextReader(reader);
        var document = new XmlDocument();
        document.Load(xmlReader);
        xmlReader.Close();
        reader.Close();

        return document;
    }
}