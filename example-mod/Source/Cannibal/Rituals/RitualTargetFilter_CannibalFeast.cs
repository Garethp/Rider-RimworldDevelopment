using AshAndDust.Cannibal.Buildings;
using RimWorld;
using Verse;

namespace AshAndDust.Cannibal.Rituals
{
    public class RitualTargetFilter_CannibalFeast: RitualObligationTargetWorker_GraveWithTarget
    {
        public RitualTargetFilter_CannibalFeast()
        {
        }

        public RitualTargetFilter_CannibalFeast(RitualObligationTargetFilterDef def) : base(def)
        {
        }
        
        protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            return target.HasThing && target.Thing is CannibalFeast thing && (thing.Corpse == obligation.targetA.Thing || thing.Corpse?.InnerPawn == obligation.targetA.Thing);
        }
    }
}