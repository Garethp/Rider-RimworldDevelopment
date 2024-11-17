// using System.Collections.Generic;
// using JetBrains.Application;
// using JetBrains.Rider.Backend.Features.ProjectModel.ProjectTemplates.DotNetExtensions;
// using JetBrains.Rider.Backend.Features.ProjectModel.ProjectTemplates.DotNetTemplates;
// using JetBrains.Rider.Model;
//
// namespace ReSharperPlugin.RimworldDev.TemplateParameters;
//
// public class ModAuthorNameParameter: DotNetTemplateParameter
// {
//     public ModAuthorNameParameter() : base("ModAuthor", "Mod Author Name", "Mod Author Name")
//     {
//     }
//
//     public override RdProjectTemplateContent CreateContent(DotNetProjectTemplateExpander expander, IDotNetTemplateContentFactory factory,
//         int index, IDictionary<string, string> context)
//     {
//         var content = factory.CreateNextParameters(new[] {expander}, index + 1, context);
//         var parameter = expander.TemplateInfo.GetParameter(Name);
//         if (parameter == null)
//         {
//             return content;
//         }
//         
//         return new RdProjectTemplateTextParameter(Name, PresentableName, null, Tooltip, RdTextParameterStyle.Text, content);
//     }
// }
//
// [ShellComponent]
// public class UnityPathParameterProvider : IDotNetTemplateParameterProvider
// {
//     public int Priority => 50;
//
//     public IReadOnlyCollection<DotNetTemplateParameter> Get()
//     {
//         return new[] {new ModAuthorNameParameter()};
//     }
// }