using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.DataContext;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml;
using JetBrains.ReSharper.Psi.Xml.Impl.Tree;
using JetBrains.ReSharper.Psi.Xml.Parsing;
using JetBrains.ReSharper.Psi.Xml.Resources;
using JetBrains.ReSharper.Psi.Xml.Tree;

namespace ReSharperPlugin.RimworldDev.Generator;

[GenerateProvider]
public class DefPropertyGeneratorItemProvider : IGenerateWorkflowProvider
{
    public IEnumerable<IGenerateActionWorkflow> CreateWorkflow(IDataContext dataContext)
    {
        yield return new GenerateDefPropertiesWorkflowXml();
    }
}

public class GenerateDefPropertiesWorkflowXml : GenerateCodeWorkflowBase
{
    public GenerateDefPropertiesWorkflowXml()
        : base(kind: "RimworldPropertyGenerator",
            icon: PsiXmlThemedIcons.XmlNode.Id,
            title: "&Property Generator",
            actionGroup: GenerateActionGroup.CLR_LANGUAGE,
            windowTitle: "Def Properties",
            description: "Add Properties to your Def",
            actionId: "Generate.DefProperties")
    {
    }

    public override double Order => 100;

    public override bool IsAvailable(IDataContext dataContext)
    {
        var solution = dataContext.GetData(ProjectModelDataConstants.SOLUTION);
        if (solution == null)
            return false;

        var sourceFile = dataContext.GetData(PsiDataConstants.SOURCE_FILE);
        if (sourceFile == null)
            return false;

        if (!sourceFile.PrimaryPsiLanguage.IsLanguage(XmlLanguage.Instance)) return false;

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

[GeneratorBuilder("RimworldPropertyGenerator", typeof(XmlLanguage))]
public class DefPropertiesGeneratorBuilderXml : GeneratorBuilderBase<GeneratorContextBase>
{
    protected override bool IsAvailable(GeneratorContextBase context)
    {
        var anchor = context.Anchor;

        if (anchor is not XmlWhitespaceToken) return false;
        if (anchor.Parent is XmlTagHeaderNode) return false;
        if (anchor.Parent?.Parent is null or IXmlFile) return false;

        return true;
    }

    protected override void BuildOptions(
        GeneratorContextBase context,
        ICollection<IGeneratorOption> options)
    {
        var hierarchy = RimworldXMLItemProvider.GetHierarchy(context.Anchor);
        var currentClass =
            RimworldXMLItemProvider.GetContextFromHierachy(hierarchy, ScopeHelper.RimworldScope, ScopeHelper.AllScopes);

        var fields = RimworldXMLItemProvider.GetAllPublicFields(currentClass, ScopeHelper.RimworldScope);

        var alreadyDefinedTags = context.Anchor.Parent
            .Children()
            .Where(child => child is XmlTag)
            .Select(tag => (tag.Children()
                .First(child => child is XmlTagHeaderNode) as XmlTagHeaderNode)?
                .ContainerName
            )
            .Where(name => name is not null)
            .ToList();

        var publicFields = fields.Where(
            field =>
                field.IsField
                && field.AccessibilityDomain.DomainType == AccessibilityDomain.AccessibilityDomainType.PUBLIC
                && !alreadyDefinedTags.Contains(field.ShortName)
                && !field.GetAttributeInstances(AttributesSource.All).Select(attribute => attribute.GetAttributeShortName()).Contains("UnsavedAttribute")
        );

        foreach (var field in publicFields)
        {
            context.ProvidedElements.Add(new GeneratorDeclaredElement(field));
        }
    }

    protected override bool HasProcessableElements(GeneratorContextBase context,
        IEnumerable<IGeneratorElement> elements)
    {
        return true;
    }

    protected override void Process(GeneratorContextBase context)
    {
        var anchor = context.Anchor;
        if (anchor is not ITreeNode) return;
        if (anchor?.Parent is not XmlTag parentTag) return;

        var factory = XmlElementFactory.GetInstance(context.Anchor);

        foreach (var inputElement in context.InputElements)
        {
            if (inputElement is not GeneratorDeclaredElement declaredElement) continue;
            var name = declaredElement.DeclaredElement.ShortName;
            var newTag = factory.CreateTagForTag(parentTag, $"<{name}></{name}>");

            ModificationUtil.AddChildAfter(anchor, newTag);

            context.OutputElements.Add(context.InputElements.First());
        }
    }
}