using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.Xml.Highlightings;
using JetBrains.ReSharper.Daemon.Xml.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;

namespace ReSharperPlugin.RimworldDev.ProblemAnalyzers;

public sealed class CustomXmlAnalysisStageProcess : XmlDaemonStageProcessBase, IRecursiveElementProcessor
{
    [NotNull] private readonly IHighlightingConsumer myConsumer;

    public CustomXmlAnalysisStageProcess(
        [NotNull] IXmlFile xmlFile, [NotNull] IDaemonProcess process,
        [NotNull] IContextBoundSettingsStore settingsStore)
        : base(xmlFile, process)
    {
        myConsumer = new FilteringHighlightingConsumer(process.SourceFile, xmlFile, settingsStore);
    }


    public override void Execute([InstantHandle] Action<DaemonStageResult> committer)
    {
        File.ProcessDescendants(this);
        committer(new DaemonStageResult(myConsumer.Highlightings));
    }

    bool IRecursiveElementProcessor.InteriorShouldBeProcessed(ITreeNode element)
    {
        return true;
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

    void IRecursiveElementProcessor.ProcessBeforeInterior(ITreeNode element)
    {
    }

    void IRecursiveElementProcessor.ProcessAfterInterior(ITreeNode element)
    {
        if (element is not XmlFloatingTextToken || element.NodeType.ToString() != "TEXT") return;

        if (element.Parent is null) return;

        if (element.Parent.Children().FirstOrDefault(x => x is XmlFloatingTextToken) != element) return;

        var hierarchy = RimworldXMLItemProvider.GetHierarchy(element);

        if (hierarchy.Count == 0) return;

        var context =
            RimworldXMLItemProvider.GetContextFromHierachy(hierarchy, ScopeHelper.RimworldScope, ScopeHelper.AllScopes);

        if (context is null) return;
        
        var fullText = String.Join("", element.Parent.Children()
            .Where(x => x is XmlFloatingTextToken or XmlWhitespaceToken)
            .Select(x => x.GetText())
            .ToList());
        
        var rangeStart = element.GetDocumentRange().TextRange.StartOffset;
        var rangeEnd = element.Parent.Children().Last(x => x is XmlFloatingTextToken or XmlWhitespaceToken)
            .GetDocumentRange().TextRange.EndOffset;
        
        var range = new DocumentRange(element.GetDocumentRange().Document, new TextRange(rangeStart, rangeEnd));
        
        switch (context.GetClrName().FullName)
        {
            case "System.Boolean":
                ProcessBoolean(fullText, range);
                return;
            case "System.Int32":
                ProcessInt(fullText, range);
                return;
            case "Verse.IntRange":
                ProcessIntRange(fullText, range);
                return;
            case "System.String":
                break;
            case "UnityEngine.Vector2":
            case "UnityEngine.Vector3":
            case "Verse.IntVec2":
                break;
            default:
                var a = 1 + 1;
                break;
        }
    }

    private void ProcessBoolean(string textValue, DocumentRange range)
    {
        if (textValue.ToLower() is "true" or "false") return;
        
        IHighlighting error = new XmlErrorHighlighting("Value must be \"true\" or \"false\"", range, Array.Empty<object>());

        myConsumer.AddHighlighting(error, range);
    }

    private void ProcessInt(string textValue, DocumentRange range)
    {
        if (int.TryParse(textValue, out _)) return;
        
        IHighlighting error = new XmlErrorHighlighting("Value must be a whole number with no decimal points", range, Array.Empty<object>());

        myConsumer.AddHighlighting(error, range);
    }
    
    private void ProcessIntRange(string textValue, DocumentRange range)
    {
        var strArray = textValue.Split('~');
        if (strArray.Length != 2)
        {
            AddError("Your value must be two integers in a format similar to \"1~2\"", range);
            return;
        }

        if (!int.TryParse(strArray[0], out _))
        {
            AddError($"\"{strArray[0]}\" is not a valid integer", range);
            return;
        }

        if (!int.TryParse(strArray[1], out _))
        {
            AddError($"\"{strArray[1]}\" is not a valid integer", range);
            return;
        }
    }

    private void AddError(string errorText, DocumentRange range)
    {
        IHighlighting error = new XmlErrorHighlighting(errorText, range, Array.Empty<object>());

        myConsumer.AddHighlighting(error, range);
    }
}