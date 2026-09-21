using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;

namespace ReSharperPlugin.RimworldDev.Tests.Navigation;

/// <summary>
/// We previously had an issue around keeping stale references to ITreeNodes in our Symbol Cache, which meant that if
/// Rider attempted to access it from our Symbol Cache (for navigation for example) after the file had been edited but
/// before we'd rebuilt our cache then we'd get an error about trying to read an uncommited PSI Document. The fix for
/// that was to store data that allows us to look up the real ITreeNode, and then do that lookup on demand rather than
/// storing the ITreeNode itself.
///
/// These tests exist to act as a regression test against that behavior coming back. 
/// </summary>
[ProjectLayouts(ProjectLayout.CSharpProject)]
[TestFileExtension(".xml")]
public class RimworldNavigationAfterEditTests(ProjectLayout layout) : RimworldNavigationTestBase(layout)
{
    private const string DefsFile = "MovingDefs.xml";
    private const string ThingALine = "    <ThingDef><defName>ThingA</defName></ThingDef>\n";

    protected override string RelativeTestDataPath => "Navigation";

    [Test] public void TestNavigateAfterDefsMove() => DoNamedTest(DefsFile);

    protected override void DoTest(Lifetime lifetime, IProject testProject)
    {
        var files = Solution.GetPsiServices().Files;
        var document = testProject.GetAllProjectFiles().Single(file => file.Name == DefsFile).ToSourceFiles().Single()
            .Document;

        files.CommitAllDocuments();
        using (ReadLockCookie.Create())
        {
            var psiFile = testProject.GetAllProjectFiles().Single(file => file.Name != DefsFile).ToSourceFiles()
                .Single().GetPrimaryPsiFile()!;

            foreach (var node in psiFile.Descendants().ToEnumerable())
            foreach (var reference in node.GetReferences<IReference>())
                reference.Resolve();
        }

        using (WriteLockCookie.Create())
            document.InsertText(document.GetText().IndexOf(" -->"), ThingALine);
        files.CommitAllDocuments();

        base.DoTest(lifetime, testProject);
    }
}