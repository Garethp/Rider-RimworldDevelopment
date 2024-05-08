using System;
using System.Linq;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Refactorings.Rename;

namespace ReSharperPlugin.RimworldDev.Refactoring;

[Language(typeof(XmlLanguage), Instantiation.DemandAnyThread)]
[Serializable]
public class RenameHelper : RenameHelperBase
{
    public override bool IsLanguageSupported => true;

    public override IRefactoringPage GetInitialPage(IRenameWorkflow renameWorkflow)
    {
        if (renameWorkflow is not RenameWorkflow workflow) return base.GetInitialPage(renameWorkflow);

        var initialName = workflow.DataModel.InitialName;
        if (!initialName.Contains("/")) return base.GetInitialPage(renameWorkflow);

        workflow.DataModel.InitialName = initialName.Split('/').Last();

        return base.GetInitialPage(renameWorkflow);
    }
}