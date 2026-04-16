using HarmonyLib;
using RimWorld;
using Verse;

namespace AshAndDust.HarmonyPatches
{
    [HarmonyPatch(typeof(IdeoFoundation), nameof(IdeoFoundation.CanAdd))]
    public class Patch_IdeoFoundation
    {
        public static void Postfix(PreceptDef precept, ref AcceptanceReport __result)
        {
            if (__result.Accepted) return;
            if (precept.issue.defName == "Ritual") return;
            if (__result.Reason != "") return;

            if (precept.exclusionTags.Contains("VanillaBurial") && precept.exclusionTags.Contains("NoVanillaCannibal"))
            {
                __result = new AcceptanceReport("Must remove Funeral ritual and add Cannibal precept before changing");
                return;
            }
            
            if (precept.exclusionTags.Contains("VanillaBurial"))
            {
                __result = new AcceptanceReport("Must remove Funeral ritual before changing");
                return;
            }

            if (precept.exclusionTags.Contains("NoHeroBurial"))
            {
                __result = new AcceptanceReport("Must remove Hero Burial ritual before changing");
                return;
            }
        }
    }
}