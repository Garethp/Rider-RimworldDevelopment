using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Build;
using JetBrains.ProjectModel.DotNetCore;
using JetBrains.ProjectModel.Impl.Build;
using JetBrains.ProjectModel.Properties;
using JetBrains.ProjectModel.Properties.Common;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ProjectModel.Properties.Managed;
using JetBrains.ProjectModel.Properties.WebSite;
using JetBrains.ProjectModel.Propoerties;
using JetBrains.Rider.Model;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;

namespace ReSharperPlugin.RimworldDev.RimworldXmlProject;

public class RimworldProjectProperties<TProjectConfiguration> : ProjectPropertiesBase<TProjectConfiguration>
    where TProjectConfiguration : ProjectConfigurationBase, new()
{
    private IBuildSettings myBuildSettings;
    private readonly ProjectLanguage myDefaultLanguage;
    private ProjectKind myProjectKind;


    internal RimworldProjectProperties(Guid factoryGuid, ProjectLanguage defaultLanguage)
        : base(factoryGuid)
    {
        this.myDefaultLanguage = ProjectLanguage.CSHARP;
        myBuildSettings = CreateBuildSettings(myDefaultLanguage);
        myProjectKind = ProjectKind.REGULAR_PROJECT;
    }
    
    public RimworldProjectProperties(ICollection<Guid> projectTypeGuids, Guid factoryGuid,
        IReadOnlyCollection<TargetFrameworkId> targetFrameworkIds,
        [CanBeNull] DotNetCorePlatformInfo dotNetCorePlatform) : base(projectTypeGuids, factoryGuid, targetFrameworkIds,
        dotNetCorePlatform)
    {
        myDefaultLanguage = ProjectLanguage.CSHARP;
        myBuildSettings = CreateBuildSettings(myDefaultLanguage);
        myProjectKind = ProjectKind.REGULAR_PROJECT;
    }

    public RimworldProjectProperties(Guid factoryGuid) : base(factoryGuid)
    {
    }

    private static IBuildSettings CreateBuildSettings(ProjectLanguage defaultLanguage)
    {
        if (defaultLanguage == ProjectLanguage.CSHARP)
            return new CSharpBuildSettings();
        return defaultLanguage == ProjectLanguage.VBASIC
            ? new VBBuildSettings()
            : (IBuildSettings)new ManagedProjectBuildSettings();
    }

    public override IBuildSettings BuildSettings => myBuildSettings;
}

[ProjectModelExtension]
public class RimworldProjectPropertiesFactory : IProjectPropertiesFactory
{
    public bool IsApplicable(ProjectPropertiesFactoryParameters parameters)
    {
        return false;
    }

    public bool IsKnownProjectTypeGuid(Guid projectTypeGuid)
    {
        return projectTypeGuid.ToString() == FactoryGuid.ToString();
    }

    public IProjectProperties CreateProjectProperties(ProjectPropertiesFactoryParameters parameters)
    {
        return new RimworldProjectProperties<CSharpProjectConfiguration>(Guid.Parse("{F2A71F9B-5D33-465A-A702-920D77279781}"), ProjectLanguage.CSHARP);
    }

    public static IProjectProperties CreateProjectProperties(
        IReadOnlyCollection<TargetFrameworkId> targetFrameworkIds,
        ProjectLanguage defaultLanguage,
        ICollection<Guid> projectTypeGuids)
    {
        return new RimworldProjectProperties<CSharpProjectConfiguration>(Guid.Parse("{F2A71F9B-5D33-465A-A702-920D77279781}"), ProjectLanguage.CSHARP);
    }
    
    public IProjectProperties Read(UnsafeReader reader)
    {
        ProjectLanguage presentableName = ProjectLanguage.ParsePresentableName(reader.ReadString());
        RimworldProjectProperties<CSharpProjectConfiguration> projectProperties =
            new RimworldProjectProperties<CSharpProjectConfiguration>(this.FactoryGuid, presentableName);
        projectProperties.ReadProjectProperties(reader);
        return projectProperties;}

    public Guid FactoryGuid => Guid.Parse("{F2A71F9B-5D33-465A-A702-920D77279781}");
}