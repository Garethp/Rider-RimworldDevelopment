using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Components;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.SymbolScope;
using ReSharperPlugin.RimworldDev.Tests.Completion;

namespace ReSharperPlugin.RimworldDev.Tests.SymbolScope;

/// <summary>
/// The def index stores offsets and finds tree nodes on demand (docs/testing-plan.md step 5). Each commit below re-runs
/// Build + Merge for the file, which used to read PSI mid-commit and log an error. After each edit, the node handed back
/// must be the one at the def's current position, not a node cached from an earlier tree.
/// </summary>
public class RimworldSymbolScopeTests : BaseTestWithSingleProject
{
    private const string FileName = "TestDefNodesFollowEdits.xml";
    private const string ThingALine = "    <ThingDef><defName>ThingA</defName></ThingDef>\n";

    protected override string RelativeTestDataPath => @"SymbolScope";

    protected override IEnumerable<string> GetReferencedAssemblies(TargetFrameworkId targetFrameworkId) =>
        base.GetReferencedAssemblies(targetFrameworkId).Concat(RimworldCompletionTestBase.RimworldReferenceAssemblies());

    [SetUp]
    public void ResetRimworldScope()
    {
        ScopeHelper.Reset();
        ScopeHelper.SkipAssemblyDiscovery = true;
    }

    [TearDown]
    public void ForgetRimworldScope() => ScopeHelper.Reset();

    [Test] public void TestDefNodesFollowEdits() => DoTestSolution(FileName);

    protected override void DoTest(Lifetime lifetime, IProject project)
    {
        var files = Solution.GetPsiServices().Files;
        var scope = Solution.GetComponent<RimworldSymbolScope>();
        var sourceFile = project.GetAllProjectFiles().Single(file => file.Name == FileName).ToSourceFiles().Single();
        var document = sourceFile.Document;

        files.CommitAllDocuments();
        AssertAllDefs(scope, sourceFile);

        // Everything moves down a line
        using (WriteLockCookie.Create())
            document.InsertText(document.GetText().IndexOf("<Defs>"), "<!-- moves every def down a line -->\n");
        files.CommitAllDocuments();
        AssertAllDefs(scope, sourceFile);

        // ThingB's name lands exactly where ThingA's was, which the index has a node cached for
        using (WriteLockCookie.Create())
        {
            var start = document.GetText().IndexOf(ThingALine);
            document.DeleteText(new TextRange(start, start + ThingALine.Length));
        }
        files.CommitAllDocuments();
        using (ReadLockCookie.Create())
        {
            Assert.That(scope.GetTagByDef("ThingDef", "ThingA"), Is.Null);
            AssertDefNode(scope.GetTagByDef("ThingDef", "ThingB"), sourceFile, ">ThingB<", 1);
        }
    }

    private static void AssertAllDefs(RimworldSymbolScope scope, IPsiSourceFile sourceFile)
    {
        using (ReadLockCookie.Create())
        {
            AssertDefNode(scope.GetTagByDef("ThingDef", "ThingA"), sourceFile, ">ThingA<", 1);
            AssertDefNode(scope.GetTagByDef("ThingDef", "ThingB"), sourceFile, ">ThingB<", 1);
            AssertDefNode(scope.GetTagByDef("ThingDef", "BaseThing"), sourceFile, "\"BaseThing\"", 0);
            Assert.That(scope.IsDefAbstract("ThingDef/BaseThing"), Is.True);
            Assert.That(scope.IsDefAbstract("ThingDef/ThingA"), Is.False);
        }
    }

    // The node must be live and sit where `marker` (plus `skip` characters) is in the document's current text
    private static void AssertDefNode(ITreeNode node, IPsiSourceFile sourceFile, string marker, int skip)
    {
        Assert.That(node, Is.Not.Null);
        Assert.That(node.IsValid(), Is.True);
        Assert.That(node.GetSourceFile(), Is.EqualTo(sourceFile));
        Assert.That(node.GetDocumentRange().StartOffset.Offset,
            Is.EqualTo(sourceFile.Document.GetText().IndexOf(marker) + skip));
    }
}
