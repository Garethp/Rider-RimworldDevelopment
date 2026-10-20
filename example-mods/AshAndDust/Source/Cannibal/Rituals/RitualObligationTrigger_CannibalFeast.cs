using RimWorld;
using Verse;

namespace AshAndDust.Cannibal.Rituals
{
    public class RitualObligationTrigger_CannibalFeast : RitualObligationTrigger
    {
        public override void Notify_MemberDied(Pawn pawn)
        {
            if (!pawn.Faction.IsPlayer && !pawn.Corpse.Map.ParentFaction.IsPlayer) return;
            
            ritual.AddObligation(new RitualObligation(ritual, (TargetInfo) (Thing) pawn.Corpse)
            {
                sendLetter = !pawn.IsSlave
            });
        }
    }
}