using System.Linq;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace ReSharperPlugin.RimworldDev.Tests.Completion;

/// <summary>
/// Dumps how the test shell sees an .xml file in the in-memory project. Exists to answer "is XML even parsed as
/// XML here?" when XML completion returns nothing.
/// </summary>
[TestFileExtension(".xml")]
public class XmlPsiDiagnosticsTests : BaseTestWithSingleProject
{
    protected override string RelativeTestDataPath => @"Completion\Xml";

    [Test] public void TestXmlIsParsed() => DoTestSolution("XmlIsParsed.xml");

    protected override void DoTest(Lifetime lifetime, IProject project)
    {
        Solution.GetPsiServices().Files.CommitAllDocuments();
        using (ReadLockCookie.Create())
        {
            var projectFile = project.GetAllProjectFiles().Single();
            var sourceFile = projectFile.ToSourceFiles().Single();
            var psiFile = sourceFile.GetPrimaryPsiFile();

            ExecuteWithGold(projectFile, writer =>
            {
                writer.WriteLine($"ProjectFileType: {projectFile.LanguageType.Name}");
                writer.WriteLine($"PrimaryPsiLanguage: {sourceFile.PrimaryPsiLanguage.Name}");
                writer.WriteLine($"PsiFile: {psiFile?.GetType().FullName ?? "<null>"}");
                writer.WriteLine($"XmlTags: {psiFile?.Descendants<IXmlTag>().ToEnumerable().Count() ?? -1}");
            });
        }
    }
}
