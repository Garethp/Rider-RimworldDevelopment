using RimWorld;
using Verse;

namespace AshAndDust.Rituals
{
    public class RitualObligationTrigger_AncestorTree : RitualObligationTrigger
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