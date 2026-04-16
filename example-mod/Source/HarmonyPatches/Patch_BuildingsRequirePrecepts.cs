using System.Linq;
using AshAndDust.Utils;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AshAndDust.HarmonyPatches
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.Visible), MethodType.Getter)]
    public class Patch_BuildingsRequirePrecepts
    {
        public static bool Prefix(ref bool __result, BuildableDef ___entDef)
        {
            if (DebugSettings.godMode) return true;
            if (___entDef is not ThingDef thingDef) return true;
            if (!thingDef.HasComp(typeof(CompRequiresPrecept))) return true;
            
            var properties = thingDef.GetCompProperties<CompRequiresPrecept_Properties>();

            var anyOf = properties.OneOf.Count < 1 || properties.OneOf.Any(
                requiredPrecept => Faction.OfPlayer.ideos.GetPrecept(requiredPrecept) != null
            );

            var allOf = properties.AllOf.Count < 1 || properties.AllOf.All(
                requiredPrecept => Faction.OfPlayer.ideos.GetPrecept(requiredPrecept) != null
            );

            var noneOf = properties.NoneOf.Count < 1 || !properties.NoneOf.Any(
                requiredPrecept => Faction.OfPlayer.ideos.GetPrecept(requiredPrecept) != null
            );

            return anyOf && allOf && noneOf;
        }
    }
}