using System;
using JetBrains.Annotations;
using JetBrains.ProjectModel.ProjectsHost.Impl;
using JetBrains.ProjectModel.ProjectsHost.Impl.FileSystem;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Update;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject.Project;


public class RimworldProjectStructureBuilder
{
    private readonly ProjectFilePropertiesFactory _projectFilePropertiesFactory;

    public RimworldProjectStructureBuilder(ProjectFilePropertiesFactory projectFilePropertiesFactory)
    {
        _projectFilePropertiesFactory = projectFilePropertiesFactory;
    }

    public void Build([NotNull] IProjectDescriptor descriptor, [CanBeNull] IFolderFilter filter)
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
                var projectFileProperties =
                    _projectFilePropertiesFactory.CreateProjectFileProperties(projectProperties);
                var projectFileDescriptor = new ProjectFileDescriptor(null, absolutePath, projectFileProperties);
                parent.Items.Add(projectFileDescriptor);
            }
        }
    }


    private bool Filter(VirtualFileSystemPath path) =>
        FileSystemWellKnownFilter.WellKnownExcludeDirectories.Contains(path.Name) ||
        FileSystemWellKnownFilter.IsWellKnownExcludeFiles(path.Name);
}