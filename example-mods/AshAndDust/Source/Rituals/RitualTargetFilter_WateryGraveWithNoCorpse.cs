using AshAndDust.Buildings;
using RimWorld;
using Verse;

namespace AshAndDust.Rituals
{
    public class RitualTargetFilter_WateryGraveWithNoCorpse: RitualObligationTargetWorker_AnyEmptyGrave
    {
        public RitualTargetFilter_WateryGraveWithNoCorpse()
        {
        }

        public RitualTargetFilter_WateryGraveWithNoCorpse(RitualObligationTargetFilterDef def) : base(def)
        {
        }

        protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            if (!target.HasThing) return false;

            if (target.Thing is ShuttleCasket shuttleCasket)
            {
                if (shuttleCasket.Corpse != null) return false;
                return shuttleCasket.GetComp<CompRefuelable>().IsFull;
            }
            
            if (target.Thing is Building_Sarcophagus grave)
            {
                if (grave.Corpse != null) return false;
                return target.Cell.GetTerrain(target.Map).IsWater;
            }
            
            return false;
        }
    }
}