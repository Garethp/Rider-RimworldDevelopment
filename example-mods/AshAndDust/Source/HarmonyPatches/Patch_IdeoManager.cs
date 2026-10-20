using System.Linq;
using AshAndDust.Comps;
using AshAndDust.Precepts;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace AshAndDust.HarmonyPatches
{
    [HarmonyPatch(typeof(IdeoManager), "Notify_PawnKilled")]
    public class Patch_IdeoManager_NotifyPawnKilled
    {
        public static void Postfix(Pawn pawn)
        {
            if (pawn?.RaceProps?.Humanlike != true) return;
            if (pawn.Corpse?.Map?.ParentFaction?.IsPlayer is null or false) return;

            BurialForNonColonist(pawn);
            CrossIdeoBurial(pawn);
        }

        public static void BurialForNonColonist(Pawn pawn)
        {
            if (pawn.Faction == Faction.OfPlayer) return;

            var ideos = Faction.OfPlayer.ideos.AllIdeos.ToList();
            foreach (var ideo in ideos)
            {
                foreach (var precept in ideo.PreceptsListForReading)
                {
                    if (Patch_Corpse_PostCorpseDestroy.TryGetPreceptComp<BurialForNonMembers>(precept) is not null)
                    {
                        ideo.Notify_MemberDied(pawn);
                    }
                }
            }
        }

        public static void CrossIdeoBurial(Pawn pawn)
        {
            if (pawn.Faction != Faction.OfPlayer) return;

            var ideos = Faction.OfPlayer.ideos.AllIdeos.ToList();
            foreach (var ideo in ideos)
            {
                if (ideo == pawn.ideo.Ideo) continue;
                
                foreach (var precept in ideo.PreceptsListForReading)
                {
                    if (Patch_Corpse_PostCorpseDestroy.TryGetPreceptComp<CrossIdeoBurial>(precept) is not null)
                    {
                        ideo.Notify_MemberDied(pawn);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Corpse), "Destroy")]
    public class Patch_Corpse_PostCorpseDestroy
    {
        public static bool Prefix(Corpse __instance)
        {
            if (__instance?.InnerPawn == null || __instance?.Map == null) return true;
            if (!__instance.InnerPawn.RaceProps.Humanlike) return true;

            if (__instance.Bugged) return true;
            var deadPawn = __instance.InnerPawn;

            if (deadPawn == null) return true;

            var mapPawns = __instance.Map.mapPawns;
            
            mapPawns.AllPawns.ForEach(mapPawn =>
            {
                if (mapPawn.Ideo == null) return;

                mapPawn.Ideo.PreceptsListForReading.ForEach(precept =>
                {
                    if (mapPawn.Faction != deadPawn.Faction)
                    {
                        TryGetPreceptComp<INotify_NonMemberCorpseDestroyed>(precept)?.Notify_NonMemberCorpseDestroyed(mapPawn, deadPawn, precept);
                    }

                    LegacyMemberCorpseDestroyed(mapPawn, deadPawn, precept);

                    if (mapPawn.Faction == deadPawn.Faction)
                    {
                        TryGetPreceptComp<INotify_MemberCorpseDestroyed>(precept)?.Notify_MemberCorpseDestroyed(mapPawn, deadPawn, precept);
                    }

                    LegacyNonMemberCorpseDestroyed(mapPawn, deadPawn, precept);
                });
            });
            
            return true;
        }

        private static void LegacyMemberCorpseDestroyed(Pawn believer, Pawn deadPerson, Precept precept)
        {
            if (TryGetPreceptComp<INotify_MemberCorpseDestroyed>(precept) is not null) return;
            if (precept.def.defName is not "TreeBurial_Colonists" 
                or "TreeBurial_WhenPossible"
                or "TreeBurial_Required") return;
            
            new TreeBurial().Notify_MemberCorpseDestroyed(believer, deadPerson, precept);
        }
        
        private static void LegacyNonMemberCorpseDestroyed(Pawn believer, Pawn deadPerson, Precept precept)
        {
            if (TryGetPreceptComp<INotify_NonMemberCorpseDestroyed>(precept) is not null) return;
            if (precept.def.defName is not "TreeBurial_Required") return;
            
            new TreeBurial_Required().Notify_NonMemberCorpseDestroyed(believer, deadPerson, precept);
        }
        
        [CanBeNull]
        public static T TryGetPreceptComp<T>(Precept precept)
        {
            if (precept.def.comps == null) return default (T);

            foreach (var c in precept.def.comps)
            {
                if (c is T comp)
                    return comp;
            }

            return default (T);
        }
    }
}