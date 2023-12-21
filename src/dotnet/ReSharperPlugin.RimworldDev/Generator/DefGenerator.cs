using System.Collections.Generic;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Resources;

namespace ReSharperPlugin.RimworldDev.Generator;

[GenerateProvider]
public class DefGeneratorItemProvider : IGenerateWorkflowProvider
{
    public IEnumerable<IGenerateActionWorkflow> CreateWorkflow(IDataContext dataContext)
    {
        yield return new GenerateDefWorkflow();
    }
}

public class GenerateDefWorkflow : GenerateCodeWorkflowBase
{
    public GenerateDefWorkflow()
        : base(kind: "DefGenerator",
            icon: PsiXmlThemedIcons.XmlNode.Id,
            title: "&GenerateDef",
            actionGroup: GenerateActionGroup.CLR_LANGUAGE,
            windowTitle: "Generate def",
            description: "Generate a Def()",
            actionId: "Generate.Def")
    {
    }

    public override double Order => 100;

    public override bool IsAvailable(IDataContext dataContext)
    {
        return true;
    }

    public override bool IsEnabled(ITreeNode context)
    {
        return true;
    }

    public override bool IsEmptyInputAllowed(IGeneratorContext context)
    {
        return true;
    }
}

[GeneratorBuilder("DefGenerator", typeof(XmlLanguage))]
public class DefGeneratorBuilder : GeneratorBuilderBase<GeneratorContextBase>
{
    protected override bool IsAvailable(GeneratorContextBase context)
    {
        return true;
    }

    protected override void BuildOptions(
        GeneratorContextBase context,
        ICollection<IGeneratorOption> options)
    {
        options.Add(new GeneratorOptionSelector("Properties", "Properties", new List<string>() { "defName", "description" }));
    }

    protected override bool HasProcessableElements(GeneratorContextBase context,
        IEnumerable<IGeneratorElement> elements)
    {
        return true;
    }
}