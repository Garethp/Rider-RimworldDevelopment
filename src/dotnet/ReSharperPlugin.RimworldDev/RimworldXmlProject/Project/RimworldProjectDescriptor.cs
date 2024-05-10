using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Search;
using JetBrains.ProjectModel.Update;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject.Project;

  public class RimworldProjectDescriptor : 
    UserDataHolder,
    IProjectDescriptor
  {
    public string Name { get; private set; }

    public VirtualFileSystemPath ProjectFilePath { get; private set; }

    public VirtualFileSystemPath Location { get; private set; }

    public Guid Guid { get; }

    public IProjectSearchDescriptor ParentProjectPointer { get; set; }

    public IProjectProperties ProjectProperties { get; }

    public void SetParentProjectPointer(IProjectSearchDescriptor parentProjectSearchDescriptor)
    {
      if (parentProjectSearchDescriptor == ParentProjectPointer)
        return;
      
      ParentProjectPointer = ParentProjectPointer == null ? parentProjectSearchDescriptor : throw new InvalidOperationException(string.Format("Parent project already defined for {0} ({1}). Existing parent: {2}. New parent: {3}", Name, ProjectFilePath, ParentProjectPointer, parentProjectSearchDescriptor));
    }

    IProjectSearchDescriptor IProjectElementSearchDescriptor.OwnerProject => this;

    public bool IsHidden { get; set; }

    public bool AllowsNonExistence => false;

    public bool IsAppDesigner => false;

    public IList<IProjectItemDescriptor> Items { get; }

    public RimworldProjectDescriptor(
      Guid guid,
      [NotNull] IProjectProperties projectProperties,
      IProjectSearchDescriptor parentProjectPointer,
      string projectName,
      VirtualFileSystemPath location,
      VirtualFileSystemPath projectFilePath)
    {
      if (projectProperties == null)
        throw new ArgumentNullException(nameof (projectProperties));
      Guid = guid;
      ParentProjectPointer = parentProjectPointer;
      ProjectProperties = projectProperties;
      Items = new List<IProjectItemDescriptor>();
      IsHidden = false;
      Name = projectName;
      Location = location;
      ProjectFilePath = projectFilePath;
    }
    
    public override string ToString()
    {
      StringBuilder sb = new StringBuilder();
      this.DumpTo(sb);
      sb.Append(string.Format(", Parent: {0}", ParentProjectPointer));
      return sb.ToString();
    }

    public ProjectKind ProjectKind => ProjectProperties.ProjectKind;

    public string ProjectName => Name;

    public Guid ProjectGuid => Guid;

    public Guid ProjectPropertiesOwnerFactoryGuid => ProjectProperties.OwnerFactoryGuid;

    public VirtualFileSystemPath ProjectFileLocation => ProjectFilePath;

    public VirtualFileSystemPath ProjectLocation => Location;

    public IReadOnlyDictionary<TargetFrameworkId, VirtualFileSystemPath> OutputPaths => null;

    public IEnumerable<KeyValuePair<string, string>> GetAdditionalInfo() => EmptyList<KeyValuePair<string, string>>.Instance;

    public TargetFrameworkId GetSingleTargetFrameworkId() => ProjectProperties.GetSingleTargetFrameworkId();

    public TargetFrameworkId GetRandomTargetFrameworkId() => ProjectProperties.GetRandomTargetFrameworkId();
  }