using System;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Update;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;

namespace ReSharperPlugin.RimworldDev.Tests.TestBases;

/// <summary>Reference resolution the way the SDK's own resolve tests check it, in the layout the fixture asks for.</summary>
public abstract class RimworldReferenceTestBase : ReferenceTestBase, IProjectLayoutFixture
{
    protected RimworldReferenceTestBase(ProjectLayout layout)
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

    // Only the plugin's references; a C# file is otherwise full of ordinary type/namespace references.
    protected override bool AcceptReference(IReference reference) =>
        reference.GetType().Assembly == typeof(ScopeHelper).Assembly;
}
