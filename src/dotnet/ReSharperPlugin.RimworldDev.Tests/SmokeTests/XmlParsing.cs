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

namespace ReSharperPlugin.RimworldDev.Tests.SmokeTests;

/// <summary>
/// The smoke tests are here to prove that tests, in general, are functioning. This is in case we do an upgrade and find
/// our automated tests are no longer functioning. It allows us to narrow it down to whether tests are broken, parsing
/// is broken, completion is broken or just our specific tests are broken.
///
/// This test is about whether we can parse XML files.
/// </summary>
[TestFileExtension(".xml")]
public class XmlParsing : BaseTestWithSingleProject
{
    protected override string RelativeTestDataPath => @"SmokeTests\Xml";

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
