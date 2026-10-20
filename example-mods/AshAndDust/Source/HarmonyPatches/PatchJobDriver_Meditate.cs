using AshAndDust.Comps;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AshAndDust.HarmonyPatches
{
    [HarmonyPatch(typeof(JobDriver_Meditate), "MeditationTick")]
    public class PatchJobDriver_Meditate
    {
        public static void Prefix(JobDriver_Meditate __instance)
        {
            if (__instance.Focus == null) return;
            var item = __instance.Focus.Thing;

            if (item.TryGetComp<MeditationSideEffectComp>() is IMeditationSideEffect comp)
            {
                comp.MeditationTick(__instance.pawn);
            }
            
            if (item is IMeditationSideEffect focusThing)
            {
                focusThing.MeditationTick(__instance.pawn);   
            }
        }
    }
}