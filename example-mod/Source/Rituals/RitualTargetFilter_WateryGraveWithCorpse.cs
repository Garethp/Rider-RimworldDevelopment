using AshAndDust.Buildings;
using RimWorld;
using Verse;

namespace AshAndDust.Rituals
{
    public class RitualTargetFilter_WateryGraveWithCorpse: RitualObligationTargetWorker_GraveWithTarget
    {
        public RitualTargetFilter_WateryGraveWithCorpse()
        {
        }

        public RitualTargetFilter_WateryGraveWithCorpse(RitualObligationTargetFilterDef def) : base(def)
        {
        }

        protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            if (!target.HasThing) return false;
            
            if (target.Thing is ShuttleCasket shuttleCasket)
            {
                if (!(shuttleCasket.Corpse == obligation.targetA.Thing || shuttleCasket.Corpse?.InnerPawn == obligation.targetA.Thing)) return false;
                return shuttleCasket.GetComp<CompRefuelable>().IsFull;
            }
            
            if (target.Thing is Building_Sarcophagus grave)
            {
                if (!(grave.Corpse == obligation.targetA.Thing || grave.Corpse?.InnerPawn == obligation.targetA.Thing)) return false;
                return target.Cell.GetTerrain(target.Map).IsWater;
            }

            return false;
        }
    }
}