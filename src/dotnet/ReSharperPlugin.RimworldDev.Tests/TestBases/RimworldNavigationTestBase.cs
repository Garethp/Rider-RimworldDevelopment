using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Update;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using JetBrains.ReSharper.IntentionsTests.Navigation;

namespace ReSharperPlugin.RimworldDev.Tests.TestBases;

/// <summary>Navigation and Find Usages the way the IDE runs them, in the layout the fixture asks for.</summary>
public abstract class RimworldNavigationTestBase : AllNavigationProvidersTestBase, IProjectLayoutFixture
{
    protected RimworldNavigationTestBase(ProjectLayout layout)
    {
        Layout = layout;
        (ProjectName, SecondProjectName) = ProjectLayoutSupport.ProjectNames(layout, ProjectName, SecondProjectName);
    }

    public ProjectLayout Layout { get; }

    /// <summary>Off for fixtures that check what happens with no RimWorld types around.</summary>
    protected virtual bool ReferenceRimworld => true;

    protected override bool CanReuseSolution(ISolution solution) =>
        ProjectLayoutSupport.CanReuse(base.CanReuseSolution(solution), solution, ProjectName);

    protected override IEnumerable<string> GetReferencedAssemblies(TargetFrameworkId targetFrameworkId) =>
        ProjectLayoutSupport.ReferencedAssemblies(base.GetReferencedAssemblies(targetFrameworkId), ReferenceRimworld);

    protected override Pair<IProjectDescriptor, IList<Pair<IProjectReferenceDescriptor, IProjectReferenceProperties>>>
        CreateProjectDescriptor(string projectName, string outputAssemblyName,
            ICollection<FileSystemPath> absoluteFileSet,
            ICollection<KeyValuePair<TargetFrameworkId, IEnumerable<string>>> libraries, Guid projectGuid,
            FileSystemPath projectLocation = null) =>
        base.CreateProjectDescriptor(projectName, outputAssemblyName, absoluteFileSet,
            ProjectLayoutSupport.Libraries(Layout, projectName, ProjectName, libraries), projectGuid, projectLocation);

    protected override void DoNamedTest(params string[] otherFiles) =>
        ProjectLayoutSupport.BuildSolution(Layout, TestName, otherFiles, RelativeTestDataPath,
            _ => base.DoNamedTest(otherFiles),
            (xmlProjectFiles, cSharpProjectFiles) => DoTestSolution(xmlProjectFiles, cSharpProjectFiles));
}
