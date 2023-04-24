using JetBrains.ReSharper.Daemon.Xml.Stages;

namespace ReSharperPlugin.RimworldDev.ProblemAnalyzers;

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Daemon.Xml.Highlightings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

  [DaemonStage(
    StagesBefore = new[] {typeof(CollectUsagesStage)},
    StagesAfter = new[] {typeof(LanguageSpecificDaemonStage)})]
  public class CustomXmlAnalysisStage : IDaemonStage
  {
    private static IXmlFile GetPrimaryXmlFile(IPsiSourceFile sourceFile)
    {
      if (sourceFile == null || !sourceFile.PrimaryPsiLanguage.Is<XmlLanguage>())
        return null;

      var xmlFile = sourceFile.GetPrimaryPsiFile() as IXmlFile;
      return xmlFile;
    }

    public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind)
    {
      var primaryXmlFile = GetPrimaryXmlFile(process.SourceFile);
      if (primaryXmlFile != null)
      {
        return FixedList.Of(new CustomXmlAnalysisStageProcess(primaryXmlFile, process, settings));
      }

      return EmptyList<IDaemonStageProcess>.InstanceList;
    }
  }
  public sealed class CustomXmlAnalysisStageProcess: XmlDaemonStageProcessBase, IRecursiveElementProcessor
  {
    [NotNull] private readonly IHighlightingConsumer myConsumer;

    public CustomXmlAnalysisStageProcess(
      [NotNull] IXmlFile xmlFile, [NotNull] IDaemonProcess process, [NotNull] IContextBoundSettingsStore settingsStore)
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
      if (element is XmlIdentifier)
      {
        IHighlighting error = new XmlErrorHighlighting("Name cannot be empty", element.GetDocumentRange(),
          Array.Empty<object>());
        myConsumer.AddHighlighting(error, element.GetDocumentRange());
      }
    }
  }