using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Rider.Backend.Features.ProjectModel.ProjectTemplates.DotNetExtensions;
using JetBrains.Rider.Backend.Features.ProjectModel.ProjectTemplates.DotNetTemplates;
using JetBrains.Rider.Model;
using JetBrains.Util;
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
        var locations = new List<string>
        {
            "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll",
            "C:\\Program Files\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll",
            "D:\\SteamLibrary\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll",
            "~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed/Assembly-CSharp.dll"
        };
        
        var possiblePaths = locations
            .Select(location => FileSystemPath.TryParse(location)).Where(location => location.ExistsFile)
            .ToArray();

        var detectedPath = possiblePaths.FirstOrDefault()?.FullPath;

        return new RdProjectTemplateTextOption(detectedPath ?? "", Name, PresentableName, Tooltip);
    }

    public override RdProjectTemplateContent CreateContent(
        DotNetProjectTemplateExpander expander,
        IDotNetTemplateContentFactory factory,
        int index, IDictionary<string, string> context
    )
    {
        var locations = new List<string>
        {
            "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll",
            "C:\\Program Files\\Steam\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll",
            "D:\\SteamLibrary\\steamapps\\common\\RimWorld\\RimWorldWin64_Data\\Managed\\Assembly-CSharp.dll",
            "~/.steam/steam/steamapps/common/RimWorld/RimWorldLinux_Data/Managed/Assembly-CSharp.dll"
        };

        var content = factory.CreateNextParameters(new[] { expander }, index + 1, context);
        var parameter = expander.TemplateInfo.GetParameter(Name);
        if (parameter == null)
        {
            return content;
        }

        var possiblePaths = locations
            .Select(location => FileSystemPath.TryParse(location)).Where(location => location.ExistsFile)
            .ToArray();

        var options = new List<RdProjectTemplateGroupOption>();

        foreach (var path in possiblePaths)
        {
            var optionContext = new Dictionary<string, string>(context) { { Name, path.FullPath } };
            var content1 = factory.CreateNextParameters(new[] { expander }, index + 1, optionContext);
            options.Add(new RdProjectTemplateGroupOption(path.FullPath, path.FullPath, null, content1));
        }

        options.Add(new RdProjectTemplateGroupOption(
            "Custom",
            possiblePaths.Any() ? "Custom" : "Custom (Assembly-CSharp.dll not found)",
            null,
            new RdProjectTemplateTextParameter(Name, "Custom path", null, Tooltip, RdTextParameterStyle.FileChooser,
                content)));

        return new RdProjectTemplateGroupParameter(Name, PresentableName,
            possiblePaths.Any() ? possiblePaths.Last().FullPath : string.Empty, null, options);
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