using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Application.DataContext;
using JetBrains.Application.Help;
using JetBrains.Application.Progress;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.DataFlow;
using JetBrains.IDE.UI;
using JetBrains.IDE.UI.Extensions;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Refactorings;
using JetBrains.ReSharper.Feature.Services.UI.Validation;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Paths;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Parsing;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Rider.Model.UIAutomation;
using JetBrains.TextControl;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.ContextActions;

[ContextAction(Description = "Move to Keyed Translation", GroupType = typeof (CSharpContextActions), Name = "StringToKeyedTranslation",
    Priority = 1)]
public class StringToKeyedTranslationAction(ICSharpContextActionDataProvider provider) : ContextActionBase
{
    public override string Text => "Move to Keyed Translation";

    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        var selectedElement = provider.GetSelectedElement<IStringLiteralOwner>();

        return (_) =>
        {
            using var lifetimeDefinition = Lifetime.Define(Lifetime.Eternal);
            var withDataRules = JetBrains.ReSharper.Resources.Shell.Shell.Instance.GetComponent<IActionManager>()
                .DataContexts.CreateWithDataRules(lifetimeDefinition.Lifetime);

            var workflow = new StringToKeyedTranslationWorkflow(solution, null, selectedElement);

            RefactoringActionUtil.ExecuteRefactoring(withDataRules, workflow);            
        };
    }

    public override bool IsAvailable(IUserDataHolder cache)
    {
        var selectedElement = provider.GetSelectedElement<IStringLiteralOwner>();

        if (selectedElement is null) return false;
        if (selectedElement.Parent is IReferenceExpression
            {
                LastChild: IIdentifier { Name: "Translate" }
            }) return false;

        if (selectedElement is IInterpolatedStringExpression interpolatedStringExpression)
        {
            return !interpolatedStringExpression.Inserts.Any(insert => insert.Expression is not IReferenceExpression);
        }

        return selectedElement is ICSharpLiteralExpression;
    }
}

public class StringToKeyedTranslationWorkflow([NotNull] ISolution solution, [CanBeNull] string actionId, ITreeNode selectedElement)
    : DrivenRefactoringWorkflow(solution, actionId)
{
    public StringToKeyedTranslationDataModel DataModel;
    public StringToKeyedTranslationDataProvider DataProvider = new ("");
    
    public override bool Initialize(IDataContext context)
    {
        return true;
    }

    public override bool IsAvailable(IDataContext context)
    {
        return true;
    }

    public override HelpId HelpKeyword => HelpId.Empty;

    public override IRefactoringPage FirstPendingRefactoringPage => new StringToKeyedTranslationRefactoringPage(this);

    public override bool MightModifyManyDocuments => true;
    public override string Title => "Move to Keyed Translation";
    public override RefactoringActionGroup ActionGroup => RefactoringActionGroup.Convert;

    public override IRefactoringExecuter CreateRefactoring(IRefactoringDriver driver)
    {
        DataModel = new StringToKeyedTranslationDataModel(selectedElement);
        
        return new StringToKeyedTranslationRefactoring(this, solution, driver);
    }
}

public class StringToKeyedTranslationDataModel(ITreeNode stringExpression) : IDataModel
{
    public ITreeNode StringExpression { get; } = stringExpression;
}

public class StringToKeyedTranslationDataProvider(string name) : IDataProvider
{
    public string Name = name;

    public bool NonInteractive => true;
}

public class StringToKeyedTranslationRefactoringPage : SingleBeRefactoringPage
{
    private readonly BeGrid content;
    private StringToKeyedTranslationWorkflow workflow;
    public IProperty<string> Name { get; }
    
    public StringToKeyedTranslationRefactoringPage(StringToKeyedTranslationWorkflow workflow) : base(workflow.WorkflowExecuterLifetime)
    {
        this.workflow = workflow;
        
        Name = new Property<string>( "StringToKeyedTranslation.Name", "");
        
        var component = workflow.GetComponent<IconHostBase>();
        var nameControl = Name.GetBeTextBox(Lifetime).WithTextNotEmpty(
            Lifetime,
            ValidationStates.validationError.GetIcon(component)
        );

        content = new BeControl[1]
        {
            nameControl.WithDescription("Key Name", Lifetime)
        }.GetGrid();
    }

    public override BeControl GetPageContent() => content;

    public override void Commit()
    {
        workflow.DataProvider = new StringToKeyedTranslationDataProvider(Name.Value);
    }
}

// TODO: If the file isn't referencing Verse yet, we need to insert that reference
// TODO: Allow the user to select the file they want to add the key to
// TODO: Add an option to insert the XML into all languages defined, not just the default language
public class StringToKeyedTranslationRefactoring(
    [NotNull] StringToKeyedTranslationWorkflow workflow,
    [NotNull] ISolution solution,
    [NotNull] IRefactoringDriver driver)
    : DrivenRefactoringBase<StringToKeyedTranslationWorkflow>(workflow, solution, driver)
{
    public override bool Execute(IProgressIndicator pi)
    {
        var selectedElement = Workflow.DataModel.StringExpression;
        if (selectedElement is not ICSharpExpression csharpExpression) return false;
        
        var textToTranslate = "";
        var arguments = new List<string>();

        if (selectedElement is ICSharpLiteralExpression literalExpression)
        {
            textToTranslate = literalExpression.GetUnquotedText();
        }

        if (selectedElement is IInterpolatedStringExpression interpolatedStringExpression)
        {
            if (!Regex.IsMatch(interpolatedStringExpression.GetUnquotedText(), "^\\$\".*"))
                return false;

            textToTranslate = Regex.Replace(interpolatedStringExpression.GetUnquotedText(), "^\\$\"(.*)$", "$1");
            
            arguments = interpolatedStringExpression
                .Inserts
                .Select(insert =>
                {
                    if (insert.Expression is not IReferenceExpression referenceExpression)
                        return "";

                    return insert.GetText();
                })
                .Where(argument => !string.IsNullOrEmpty(argument))
                .Distinct()
                .ToList();
        }
        
        var translationKey = Workflow.DataProvider.Name;
        
        var invocation = CSharpElementFactory
            .GetInstance(csharpExpression)
            .CreateExpression($"\"$0\".Translate({String.Join(", ", arguments)})", Workflow.DataProvider.Name);

        var rimworldProject = Solution
            .GetTopLevelProjects()
            .FirstOrDefault(project => project.ProjectFileLocation.FullPath.EndsWith("About.xml"));
        
        if (rimworldProject == null)
            return false;
        
        var languagesFolder = rimworldProject.GetSubItems().FirstOrDefault(item => item.Name == "Languages");
        if (languagesFolder == null)
            return false;
        
        var translationFiles = rimworldProject
            .GetAllProjectFiles()
            .Where(file => file.Location.FullPath.StartsWith(languagesFolder.Location.FullPath));
        
        var firstFile = translationFiles.FirstOrDefault();
        if (firstFile == null)
            return false;
        
        var psiFile = rimworldProject.GetPsiSourceFileInProject(firstFile.Location).GetPrimaryPsiFile();
        if (psiFile is not XmlFile xmlFile)
            return false;
        
        var languageDataTag = xmlFile.GetNestedTags<IXmlTag>("LanguageData").FirstOrDefault();
        if (languageDataTag == null)
            return false;
        
        var lastTag = languageDataTag.Children().Last(tag => tag is IXmlTag);
        
        for (var index = 0; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            
            textToTranslate = textToTranslate.FullReplace("{" + argument + "}", "{" + index + "}");
        }
        
        var newTag = XmlElementFactory.GetInstance(languageDataTag).CreateTagForTag(languageDataTag, $"<{translationKey}>{textToTranslate}</{translationKey}>");
        
        csharpExpression.ReplaceBy(invocation);
        ModificationUtil.AddChildAfter(languageDataTag, lastTag, newTag);
        
        return true;
    }
}