using System.Collections.Generic;
using System.IO;
using JetBrains.Application;
using JetBrains.Rider.Backend.Features.ProjectModel.ProjectTemplates.DotNetExtensions;
using JetBrains.Rider.Model;
using Microsoft.TemplateEngine.Abstractions;

namespace ReSharperPlugin.RimworldDev.TemplateParameters;

public class RimworldDLLParameter : DotNetTemplateParameter
{
    public RimworldDLLParameter() : base("RimworldDLL", "Rimworld DLL", "Path to Assembly-CSharp.dll")
    {
    }

    public override RdProjectTemplateOption CreateContent(ITemplateInfo templateInfo, ITemplateParameter templateParameter,
        Dictionary<string, object> context)
    {
        var detectedPath = ScopeHelper.FindRimworldDll(Directory.GetCurrentDirectory())?.FullPath;
        
        return new RdProjectTemplateTextOption(detectedPath ?? "",  "string", Name, PresentableName, Tooltip);
    }
}

[ShellComponent]
public class RimworldDLLParameterProvider : IDotNetTemplateParameterProvider
{
    public int Priority => 50;

    public IReadOnlyCollection<DotNetTemplateParameter> Get()
    {
        return new[] { new RimworldDLLParameter() };
    }
}