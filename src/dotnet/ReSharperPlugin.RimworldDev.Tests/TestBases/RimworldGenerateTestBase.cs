using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Update;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.FeaturesTestFramework.Generate;
using JetBrains.ReSharper.Psi;
using JetBrains.TextControl;

namespace ReSharperPlugin.RimworldDev.Tests.TestBases;

/// <summary>The Generate menu the way the IDE runs it, in the layout the fixture asks for.</summary>
public abstract class RimworldGenerateTestBase : GenerateTestBase, IProjectLayoutFixture
{
    protected RimworldGenerateTestBase(ProjectLayout layout)
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

    // GenerateTestBase decapitalises the first test file, and the gold is named after the file it ends up with
    protected override void DoNamedTest(params string[] otherFiles) =>
        ProjectLayoutSupport.BuildSolution(Layout, ModifyTestFiles([TestName]).Single(), otherFiles,
            RelativeTestDataPath,
            _ => base.DoNamedTest(otherFiles),
            (xmlProjectFiles, cSharpProjectFiles) => DoTestSolution(xmlProjectFiles, cSharpProjectFiles));

    // The generator writes tags straight into the tree, which in the IDE runs under the action's own write lock
    protected override void DoTest(Lifetime lifetime, IProject testProject)
    {
        using (WriteLockCookie.Create())
            base.DoTest(lifetime, testProject);
    }

    // The generator reads the scopes from ScopeHelper's statics, which in the IDE another feature has filled in by now
    protected override IGeneratorWorkflow CreateWorkflow(string kind, ITextControl textControl,
        IPsiSourceFile sourceFile)
    {
        ScopeHelper.UpdateScopes(Solution);

        return base.CreateWorkflow(kind, textControl, sourceFile);
    }
}
