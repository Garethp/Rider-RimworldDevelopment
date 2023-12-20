using System.Collections.Generic;
using JetBrains.Application.DataContext;
using JetBrains.ReSharper.Feature.Services.CSharp.Generate;
using JetBrains.ReSharper.Feature.Services.Generate;
using JetBrains.ReSharper.Feature.Services.Generate.Actions;
using JetBrains.ReSharper.Feature.Services.Generate.Workflows;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Xml.Resources;
using JetBrains.Util;

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
    // Using named parameters for clarification
    public GenerateDefWorkflow()
        : base(kind: "DefGenerator", icon: PsiXmlThemedIcons.XmlNode.Id, title: "&GenerateDef",
            actionGroup: GenerateActionGroup.CLR_LANGUAGE,
            windowTitle: "Generate def",
            description: "Generate a Def()",
            actionId: "Generate.Def")
    {
    }

    public override double Order
    {
        get { return 100; }
    }

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

[GeneratorBuilder("DefGenerator", typeof(CSharpLanguage))]
public class DefGeneratorBuilder : GeneratorBuilderBase<CSharpGeneratorContext>
{
    protected override bool IsAvailable(CSharpGeneratorContext context)
    {
        return true;
    }
    
    protected override void BuildOptions(
        CSharpGeneratorContext context,
        ICollection<IGeneratorOption> options)
    {
        var scope = ScopeHelper.RimworldScope;
        if (scope is null) return;
        var def = scope.GetTypeElementByCLRName("Verse.Def");

        
        // if ((context.ClassDeclaration.DeclaredElement as IClass).IsStaticClass() || !(context.Kind == "Constructor"))
            // return;

            var defOption = new GeneratorOptionSelector("Properties", "Properties",
                new List<string>() { "defName", "description" });

            defOption.HasDependentOptions = true;
            
        options.Add(defOption);
        context.ProvidedElements.Add(new GeneratorDeclaredElement(def.Fields.FirstNotNull()));
        // options.Add((IGeneratorOption) new GeneratorOptionSelector("AccessRights", JetBrains.ReSharper.Feature.Services.CSharp.Resources.Strings.GenerateOption_AccessRights_Text, CSharpBuilderOptions.GetAccessRightsChoicesWithAutomatic(context)));
    }

    protected override bool HasProcessableElements(CSharpGeneratorContext context, IEnumerable<IGeneratorElement> elements)
    {
        return true;
    }
}

// [GeneratorElementProvider("DefGenerator", typeof(CSharpLanguage))]
// public class DefGeneratorElementProvider : GeneratorProviderBase<CSharpGeneratorContext>
// {
//     public override void Populate(CSharpGeneratorContext context)
//     {
//         var scope = ScopeHelper.RimworldScope;
//         if (scope is null) return;
//
//         var def = scope.GetTypeElementByCLRName("Verse.Def");
//         var a = 1 + 1;
//         context.ProvidedElements.Add(new GeneratorDeclaredElement(def.Fields.FirstNotNull()));
//     }
// }