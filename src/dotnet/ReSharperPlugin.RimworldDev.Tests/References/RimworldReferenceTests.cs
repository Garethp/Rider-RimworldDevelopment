using JetBrains.Application.Components;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;
using ReSharperPlugin.RimworldDev.Tests.TestBases;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReSharperPlugin.RimworldDev.Tests.References;

/// <summary>
/// Ctrl+Click, tested without the SDK's navigation machinery: walk every node of the test file, ask it for its
/// references (which is where our IReferenceProviderFactory implementations plug in), resolve each one and dump
/// "node → reference type → what it resolved to". The first file given to DoTestSolution is the one dumped; the rest
/// only exist to be resolved into.
/// </summary>
[ProjectLayouts(ProjectLayout.CSharpProject)]
public class RimworldReferenceTests(ProjectLayout layout) : RimworldSolutionTestBase(layout)
{
    protected override string RelativeTestDataPath => @"References";

    [Test] public void TestXmlToCSharp() => DoTestSolution("XmlToCSharp.xml");
    [Test] public void TestXmlToXmlDef() => DoTestSolution("XmlToXmlDef.xml", "OtherDefs.xml");
    [Test] public void TestCSharpToXmlDef() => DoTestSolution("CSharpToXmlDef.cs", "CSharpDefs.xml");

    protected override void DoTest(Lifetime lifetime, IProject project)
    {
        Solution.GetPsiServices().Files.CommitAllDocuments();
        using (ReadLockCookie.Create())
        {
            var dumpedFileName = TestMethodName2FileNames().First();
            var projectFile = project.GetAllProjectFiles().Single(file => file.Name == dumpedFileName);
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

    private IEnumerable<string> TestMethodName2FileNames() => myFileSet;

    private string[] myFileSet = [];

    protected new void DoTestSolution(params string[] fileSet)
    {
        myFileSet = fileSet;
        DoLayoutTestSolution(fileSet);
    }

    private static string Describe(IDeclaredElement element) => element switch
    {
        null => "<nothing>",
        ITypeElement type => $"type {type.GetClrName().FullName}",
        ITypeMember member => $"{member.GetElementType().PresentableName} {member.ContainingType?.GetClrName().FullName}.{member.ShortName}",
        _ => $"{element.GetType().Name} {element.ShortName}",
    };
}
