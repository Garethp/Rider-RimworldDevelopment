using System.Linq;

namespace ReSharperPlugin.RimworldDev;

public class DefNameValue
{
    public readonly string DefType;
    
    public readonly string DefName;

    public string TagId => $"{DefType}/{DefName}";

    public DefNameValue(string tagId)
    {
        var split = tagId.Split('/');
        DefType = split.First();
        DefName = split.Last();
    }
    
    public DefNameValue(string defType, string defName)
    {
        DefType = defType;
        DefName = defName;
    }
}