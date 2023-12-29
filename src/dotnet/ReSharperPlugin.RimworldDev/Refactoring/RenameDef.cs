using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.Refactorings.Specific.Rename;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Refactorings.Rename;
using JetBrains.TextControl;
using JetBrains.TextControl.DataContext;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.Refactoring;

[RefactoringWorkflowProvider]
public class RenameDefProvider : IRefactoringWorkflowProvider
{
    public IEnumerable<IRefactoringWorkflow> CreateWorkflow(IDataContext dataContext)
    {
        var solution = dataContext.GetData(ProjectModelDataConstants.SOLUTION);
        yield return new RenameDefWorkflow(solution, "RenameXmlDef");
    }
}

public class RenameDefWorkflow(ISolution solution, string actionId) : RenameWorkflowBase(solution, actionId)
{
    public override bool IsAvailable(IDataContext context)
    {
        return true;
    }

    private List<IDeclaredElement> GetDeclaredElements(IDataContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var declaredElements = new List<IDeclaredElement>();
        var data1 = context.GetData(TextControlDataConstants.TEXT_CONTROL);

        DataProvider = DataProvider ?? context.GetData(RenameRefactoringService.RenameDataProvider);
        if ((DataProvider == null || DataProvider.CanBeLocal) && data1 != null &&
            RenameRefactoringService.Instance.CheckLocalRenameAvailability(context))
            return new();
        ICollection<IDeclaredElement> data2 =
            context.GetData(RenameRefactoringService.PRIMEVAL_DECLARED_ELEMENTS_TO_RENAME);
        if (data2.IsNullOrEmpty())
            return new();

        // @TODO: Check, can we make a rename factory to just **work** with the in-built renaming
        declaredElements = data2.Where(x => RenameRefactoringService.Instance.CheckRenameAvailability(x) == RenameAvailabilityCheckResult.CanBeRenamed).ToList();
        // declaredElements = data2.ToList();
        return declaredElements;
    }

    public override bool Initialize(IDataContext context)
    {
        var declaredElements = GetDeclaredElements(context);
        if (declaredElements.Count == 0)
            return false;

        if (DataModel != null)
            return true;
        var primaryDeclaredElement = declaredElements.First();
        var fileRenames = RenameRefactoringService.Instance.FileRenameProviders
            .SelectMany(provider => provider.GetFileRenames(primaryDeclaredElement, primaryDeclaredElement.ShortName))
            .AsCollection();

        var canHaveFileRenames = fileRenames.IsEmpty() ? RenameFilesOption.NothingToRename :
            fileRenames.All(r => r.AlwaysMustBeRenamed) ? RenameFilesOption.RenameWithoutConfirmation :
            RenameFilesOption.RenameWithConfirmation;

        var renameHelperBase = LanguageSpecific[RenameUtil.GetPsiLanguageTypeOrKnownLanguage(primaryDeclaredElement)];
        var data = context.GetData(PsiDataConstants.REFERENCE);
        var occurrence = new RenameWorkflowPopupOccurrence(Strings.RenameThisOverload_Text,
            Strings.RenameInitialElementOnly_Text, new IDeclaredElement[1] { declaredElements.FirstOrDefault() });

        var workflowPopupOccurrenceArray1 = new List<RenameWorkflowPopupOccurrence>()
        {
            occurrence,
            new(Strings.RenameAllOverloads_Text, Strings.RenameInitialElementAndAllItsOverloads_Text, declaredElements)
        }.ToArray();
        // var workflowPopupOccurrenceArray1 = renameHelperBase.GetPopupOccurences(declaredElements.FirstOrDefault()).AsArray();
        if (workflowPopupOccurrenceArray1.Length > 1)
        {
            RenameWorkflowPopupOccurrence workflowPopupOccurrence = null;
            if (DataProvider == null)
            {
                workflowPopupOccurrence = ShowOccurrences(workflowPopupOccurrenceArray1, context);
            }
            else
            {
                int usages = DataProvider.Usages;
                RenameWorkflowPopupOccurrence[] workflowPopupOccurrenceArray2 = workflowPopupOccurrenceArray1.AsArray();
                if (usages >= 0 && workflowPopupOccurrenceArray2.Length > usages)
                    workflowPopupOccurrence = workflowPopupOccurrenceArray2[usages];
            }

            if (workflowPopupOccurrence == null)
                return false;
            IEnumerable<IDeclaredElement> second = workflowPopupOccurrence.Elements.SelectNotNull(
                (Func<IDeclaredElementPointer<IDeclaredElement>, IDeclaredElement>)(dep => dep.FindDeclaredElement()));
            declaredElements = declaredElements.Concat(second).AsList();
        }

        DataModel = new RenameDataModel(declaredElements, data, canHaveFileRenames, WorkflowExecuterLifetime, Solution,
            renameHelperBase.GetOptionsModel(primaryDeclaredElement, data, WorkflowExecuterLifetime));
        return true;
    }

    public override IRefactoringExecuter CreateRefactoring(IRefactoringDriver driver)
    {
        return new RenameRefactoring(this, this.Solution, driver);
    }
};

public class RenameDef(RenameWorkflowBase workFlow, ISolution solution, IRefactoringDriver driver)
    : RenameRefactoring(workFlow, solution, driver)
{
    public override bool Execute(IProgressIndicator pi)
    {
        return base.Execute(pi);
    }
};