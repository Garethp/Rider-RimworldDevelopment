using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace ReSharperPlugin.RimworldDev.Tests.TestBases;

/// <summary>
/// Ctrl+Click, tested without the SDK's navigation machinery: walk every node of the test file, ask it for its
/// references (which is where our IReferenceProviderFactory implementations plug in), resolve each one and dump
/// "node → reference type → what it resolved to". The file named after the test is the one dumped; the files given to
/// DoNamedTest only exist to be resolved into.
/// </summary>
public abstract class RimworldReferenceTestBase(ProjectLayout layout) : RimworldSolutionTestBase(layout)
{
    protected void DoNamedTest(params string[] otherFiles) =>
        ProjectLayoutSupport.BuildSolution(Layout, TestName, otherFiles, RelativeTestDataPath,
            files => DoTestSolution(files),
            (xmlProjectFiles, cSharpProjectFiles) => DoTestSolution(xmlProjectFiles, cSharpProjectFiles));

    protected override void DoTest(Lifetime lifetime, IProject project)
    {
        Solution.GetPsiServices().Files.CommitAllDocuments();
        using (ReadLockCookie.Create())
        {
            var projectFile = project.GetAllProjectFiles().Single(file => file.Name == TestName);
            var sourceFile = projectFile.ToSourceFiles().Single();
            var psiFile = sourceFile.GetPrimaryPsiFile()!;
            var document = sourceFile.Document;

            ExecuteWithGold(projectFile, writer =>
            {
                foreach (var node in psiFile.Descendants().ToEnumerable())
                {
                    // Only the plugin's references; a C# file is otherwise full of ordinary type/namespace references.
                    foreach (var reference in node.GetReferences<IReference>()
                                 .Where(reference => reference.GetType().Assembly == typeof(ScopeHelper).Assembly))
                    {
                        var start = document.GetCoordsByOffset(reference.GetDocumentRange().StartOffset.Offset);
                        var resolved = reference.Resolve();
                        writer.WriteLine(
                            $"({(int)start.Line + 1},{(int)start.Column + 1}) '{reference.GetDocumentRange().GetText()}' " +
                            $"[{reference.GetType().Name}] -> {resolved.ResolveErrorType}: {Describe(resolved.DeclaredElement)}");
                    }
                }
            });
        }
    }

    private static string Describe(IDeclaredElement element) => element switch
    {
        null => "<nothing>",
        ITypeElement type => $"type {type.GetClrName().FullName}",
        ITypeMember member => $"{member.GetElementType().PresentableName} {member.ContainingType?.GetClrName().FullName}.{member.ShortName}",
        _ => $"{element.GetType().Name} {element.ShortName}",
    };
}
