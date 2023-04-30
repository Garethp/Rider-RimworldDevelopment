namespace ReSharperPlugin.RimworldDev.ProblemAnalyzers;

using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures.Collections;

[DaemonStage(
    StagesBefore = new[] { typeof(CollectUsagesStage) },
    StagesAfter = new[] { typeof(LanguageSpecificDaemonStage) })]
public class CustomXmlAnalysisStage : IDaemonStage
{
    private static IXmlFile GetPrimaryXmlFile(IPsiSourceFile sourceFile)
    {
        if (sourceFile == null || !sourceFile.PrimaryPsiLanguage.Is<XmlLanguage>())
            return null;

        var xmlFile = sourceFile.GetPrimaryPsiFile() as IXmlFile;
        return xmlFile;
    }

    public IEnumerable<IDaemonStageProcess> CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings,
        DaemonProcessKind processKind)
    {
        var primaryXmlFile = GetPrimaryXmlFile(process.SourceFile);
        if (primaryXmlFile != null)
        {
            return FixedList.Of(new CustomXmlAnalysisStageProcess(primaryXmlFile, process, settings));
        }

        return EmptyList<IDaemonStageProcess>.InstanceList;
    }
}