using System;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.AspRouteTemplates.Highlightings;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.CSharp.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using ReSharperPlugin.RimworldDev.SymbolScope;

namespace ReSharperPlugin.RimworldDev.ProblemAnalyzers;

[DaemonStage(
    Instantiation.DemandAnyThreadSafe,
    StagesBefore = [typeof(LanguageSpecificDaemonStage), typeof(CollectUsagesStage)],
    HighlightingTypes = [typeof(KeyedTranslationAnalysisProcessStage)]
)]
public class KeyedTranslationAnalysisStage : CSharpDaemonStageBase
{
    protected override IDaemonStageProcess CreateProcess(
        IDaemonProcess process,
        IContextBoundSettingsStore settings,
        DaemonProcessKind processKind,
        ICSharpFile file
    )
    {
        return new KeyedTranslationAnalysisProcessStage(process, file);
    }

    [HighlightingSource(HighlightingTypes = [typeof (MethodMissingRouteParametersHighlighting)])]
    public class KeyedTranslationAnalysisProcessStage : CSharpDaemonStageProcessBase, IRecursiveElementProcessor
    {
        [NotNull] private readonly IHighlightingConsumer myConsumer;
        [NotNull] private readonly RimworldKeyedTranslationSymbolScope symbolScope;

        public KeyedTranslationAnalysisProcessStage([NotNull] IDaemonProcess process,
            [NotNull] ICSharpFile file) : base(process, file)
        {
            symbolScope = file.GetSolution().GetComponent<RimworldKeyedTranslationSymbolScope>();
            
            myConsumer = new FilteringHighlightingConsumer(
                DaemonProcess.SourceFile, 
                File, 
                DaemonProcess.ContextBoundSettingsStore
            );
        }

        public override void Execute(Action<DaemonStageResult> committer)
        {
            File.ProcessDescendants(this);
            committer(new DaemonStageResult(myConsumer.CollectHighlightings()));
        }

        public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            return true;
        }

        void IRecursiveElementProcessor.ProcessBeforeInterior(ITreeNode element)
        {
        }

        public void ProcessAfterInterior(ITreeNode element)
        {
            if (element is not IIdentifier { Name: "Translate" }) return;
            if (element.Parent?.Parent is not IInvocationExpression invocation) return;
            if (element.Parent?.FirstChild is not ICSharpLiteralExpression translationStringElement) return;

            var translationKey = translationStringElement.GetUnquotedText();
            var arguments = invocation.ArgumentList;
            var argumentCount = arguments.Arguments.Count;

            if (!symbolScope.HasTranslationKey(translationKey))
            {
                AddError("Translation key does not exist", translationStringElement.GetDocumentRange());
                return;
            }

            var translation = symbolScope.GetTranslationKey(translationKey).Value!.Tag.InnerText;

            var matches = Regex.Matches(translation, @"(\{\d+\})");

            if (matches.Count != argumentCount)
            {
                AddError(
                    $"Invalid number of arguments. Expected {matches.Count}, Passed in {arguments.Arguments.Count}",
                    invocation.GetDocumentRange()
                );
                return;
            }
        }

        bool IRecursiveElementProcessor.ProcessingIsFinished
        {
            get
            {
                if (base.DaemonProcess.InterruptFlag)
                    throw new OperationCanceledException();
                return false;
            }
        }
        
        private void AddError(string errorText, DocumentRange range)
        {
            IHighlighting error = new KeyedTranslationHighlighting(errorText, range);

            myConsumer.AddHighlighting(error, range);
        }
    }

    [StaticSeverityHighlighting(Severity.ERROR, typeof(CSharpErrors))]
    public class KeyedTranslationHighlighting(string tooltip, DocumentRange range) : IHighlighting
    {
        public bool IsValid() => true;

        public DocumentRange CalculateRange() => range;

        public string ToolTip => tooltip;
        public string ErrorStripeToolTip => "test error";
    }
}