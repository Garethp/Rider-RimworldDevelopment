using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions;
using JetBrains.Application.UI.Actions.MenuGroups;
using JetBrains.Application.UI.ActionsRevised.Menu;
using JetBrains.Application.UI.ActionSystem.ActionsRevised.Menu;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties.CSharp;
using JetBrains.ReSharper.UnitTestFramework.Execution.Launch.Stages;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.Migrator;

public abstract class SampleAction : IExecutableAction
{
    public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
    {
        return true;    // function result indicates whether the menu item is enabled or disabled
    }

    public void Execute(IDataContext context, DelegateExecute nextExecute)
    {
        RunAction(context, nextExecute);
    }

    protected abstract void RunAction(IDataContext context, DelegateExecute nextExecute);
}

[Action("RunMigrationSampleAction", "Run Migration Sample", Id = 543210)]
public class ActionShowMessageBox : SampleAction, IInsertLast<MainMenuFeaturesGroup>
{
    protected override void RunAction(IDataContext context, DelegateExecute nextExecute)
    {
        var solution = context.GetData(JetBrains.ProjectModel.DataContext.ProjectModelDataConstants.SOLUTION);
        
        var rimworldProjects = solution.GetAllProjects()
            .Where(project => project.ProjectProperties is CSharpProjectProperties)
            .Where(project =>
            {
                var targetFrameworkId = project.GetCurrentTargetFrameworkId();

                return project.GetModuleReferences(targetFrameworkId).Any(module => module.Name == "Assembly-CSharp");
            }).ToList();

        
        var target = rimworldProjects.First();
        
        MessageBox.ShowInfo(solution?.SolutionFile != null
            ? $"{solution.SolutionFile?.Name} solution is opened"
            : "No solution is opened");

        var a = 1 + 1;
    }
}