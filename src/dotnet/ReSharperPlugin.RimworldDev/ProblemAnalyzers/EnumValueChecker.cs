using System;
using JetBrains.ReSharper.Daemon.Xml.Highlightings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;

namespace ReSharperPlugin.RimworldDev.ProblemAnalyzers;

[ElementProblemAnalyzer(new[] { typeof(XmlIdentifier) },
    HighlightingTypes = new[] { typeof(XmlErrorHighlighting) })]
public class EnumValueChecker : ElementProblemAnalyzer<XmlIdentifier>
{
    protected override void Run(XmlIdentifier element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
            IHighlighting error = new XmlErrorHighlighting("Name cannot be empty", element.GetDocumentRange(),
            Array.Empty<object>());
        consumer.AddHighlighting(error, element.GetDocumentRange());
    }
}