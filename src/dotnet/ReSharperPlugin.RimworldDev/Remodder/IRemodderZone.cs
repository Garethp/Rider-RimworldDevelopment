using System.Threading.Tasks;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;

namespace ReSharperPlugin.RimworldDev.Remodder
{
    [ZoneDefinition]
    // [ZoneDefinitionConfigurableFeature("Title", "Description", IsInProductSection: false)]
    public interface IRemodderZone : IZone
    {
    }
}
