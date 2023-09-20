using System;
using System.Linq;
using System.Text.RegularExpressions;
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
            case "System.Single":
                ProcessFloat(fullText, range);
                return;
            case "Verse.IntRange":
                ProcessIntRange(fullText, range);
                return;
            case "Verse.FloatRange":
                ProcessFloatRange(fullText, range);
                return;
            case "System.String":
                break;
            case "UnityEngine.Vector2":
                ProcessVector2(fullText, range);
                break;
            case "UnityEngine.Vector3":
                ProcessVector3(fullText, range);
                break;
            case "UnityEngine.Rect":
            case "UnityEngine.Color":
            case "RimWorld.PsychicDroneLevel":
            case "RimWorld.FoodTypeFlags":
            case "Verse.DevelopmentalStage":
            case "Verse.ColorInt":
            case "Verse.IntVec3":
            case "Verse.IntVec2":
            case "Verse.WorkTags":
                break;
            default:
                if (context.GetType().Name == "Enum")
                {
                    ProcessEnum(fullText, range, context);
                    break;
                }

                if (context.GetType().Name == "Struct")
                {
                    ProcessStruct(fullText, range, context);
                    break;
                }
                
                var a = 1 + 1;
                break;
        }
    }

    private void ProcessBoolean(string textValue, DocumentRange range)
    {
        if (textValue.ToLower() is "true" or "false") return;
        
        AddError("Value must be \"true\" or \"false\"", range);
    }

    private void ProcessInt(string textValue, DocumentRange range)
    {
        if (int.TryParse(textValue, out _)) return;
        
        AddError("Value must be a whole number with no decimal points", range);
    }
    
    private void ProcessFloat(string textValue, DocumentRange range)
    {
        if (float.TryParse(textValue, out _)) return;
        
        AddError("Value must be a valid number", range);
    }
    
    private void ProcessIntRange(string textValue, DocumentRange range)
    {
        var strArray = textValue.Split('~');
        
        // For some reason IntRange also just accepts a single number instead of a range
        if (strArray.Length == 1 && int.TryParse(strArray[0], out _)) return;
        
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
    
    private void ProcessFloatRange(string textValue, DocumentRange range)
    {
        var strArray = textValue.Split('~');
        // For some reason FloatRange also just accepts a single number instead of a range
        if (strArray.Length == 1 && float.TryParse(strArray[0], out _)) return;
        
        if (strArray.Length != 2)
        {
            AddError("Your value must be two integers in a format similar to \"1~2\"", range);
            return;
        }

        if (!float.TryParse(strArray[0], out _))
        {
            AddError($"\"{strArray[0]}\" is not a valid float", range);
            return;
        }

        if (!float.TryParse(strArray[1], out _))
        {
            AddError($"\"{strArray[1]}\" is not a valid float", range);
            return;
        }
    }

    private void ProcessVector2(string textValue, DocumentRange range)
    {
        var match = Regex.Match(textValue.Trim(), @"^\((.*?),(.*?)\)$");
        if (!match.Success)
        {
            // For some reason Vector2 allows us to pass in just a single number instead of a vector?
            if (float.TryParse(textValue, out _)) return;
            
            AddError("Your value must be in a format similar to (1,2)", range);
            return;
        }

        var firstValue = match.Groups[1].Value;
        var secondValue = match.Groups[2].Value;

        if (!float.TryParse(firstValue, out _))
        {
            AddError($"\"{firstValue}\" is not a valid number", range);
            return;
        }

        if (!float.TryParse(secondValue, out _))
        {
            AddError($"\"{secondValue}\" is not a valid number", range);
            return;
        }
    }
    
    private void ProcessVector3(string textValue, DocumentRange range)
    {
        var match = Regex.Match(textValue.Trim(), @"^\((.*?),(.*?),(.*?)\)$");
        if (!match.Success)
        {
            AddError("Your value must be in a format similar to (1,2,3)", range);
            return;
        }

        var firstValue = match.Groups[1].Value;
        var secondValue = match.Groups[2].Value;
        var thirdValue = match.Groups[3].Value;

        if (!float.TryParse(firstValue, out _))
        {
            AddError($"\"{firstValue}\" is not a valid number", range);
            return;
        }

        if (!float.TryParse(secondValue, out _))
        {
            AddError($"\"{secondValue}\" is not a valid number", range);
            return;
        }

        if (!float.TryParse(thirdValue, out _))
        {
            AddError($"\"{thirdValue}\" is not a valid number", range);
            return;
        }
    }

    private void ProcessEnum(string textValue, DocumentRange range, ITypeElement context)
    {
        if (context.GetType().Name != "Enum") return;
        
        var enumValues = context.GetMembers().Select(x => x.ShortName).ToList();
        if (!enumValues.Contains(textValue))
        {
            AddError($"\"{textValue}\" is not a valid value for {context.ShortName}", range);
        }
    }
    
    private void ProcessStruct(string textValue, DocumentRange range, ITypeElement context)
    {
        if (context.GetType().Name != "Struct") return;
        
        var enumValues = context.GetMembers().Select(x => x.ShortName).ToList();
        if (!enumValues.Contains(textValue))
        {
            AddError($"\"{textValue}\" is not a valid value for {context.ShortName}", range);
        }
    }
    
    private void AddError(string errorText, DocumentRange range)
    {
        IHighlighting error = new XmlErrorHighlighting(errorText, range, Array.Empty<object>());

        myConsumer.AddHighlighting(error, range);
    }
}