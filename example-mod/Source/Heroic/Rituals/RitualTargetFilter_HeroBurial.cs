using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AshAndDust.Heroic.Rituals
{
    public class RitualTargetFilter_HeroBurial: RitualObligationTargetWorker_GraveWithTarget
    {
        private RitualTargetFilter_HeroBurialDef Props => (RitualTargetFilter_HeroBurialDef) def;
        
        public RitualTargetFilter_HeroBurial()
        {
        }

        public RitualTargetFilter_HeroBurial(RitualObligationTargetFilterDef def) : base(def)
        {
        }
        
        protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            var baseReport = base.CanUseTargetInternal(target, obligation);
            if (!baseReport.canUse) return baseReport;

            var grave = (Building_Grave) target.Thing;
            var pawn = grave.Corpse.InnerPawn;
         
            var room = target.Thing.GetRoom();
            if (room.Role.defName != "Tomb") return "Must be inside a Tomb";

            if (Props.GetCurrentRequirement(pawn).Any(requirement => !requirement.Met(room)))
            {
                return "Room requirements not met";
            }

            return true;
        }

        public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
        {
            foreach (var targetInfo in base.GetTargetInfos(obligation))
            {
                yield return targetInfo;
            }

            if (obligation == null)
            {
                yield return "A tomb that meets the required expectations";
                yield break;
            }
            var pawn = obligation.targetA.Thing is Pawn ? (Pawn) obligation.targetA.Thing : ((Corpse) obligation.targetA.Thing).InnerPawn;
            foreach (var roomRequirement in Props.GetCurrentRequirement(pawn))
            {
                yield return roomRequirement.LabelCap();
            }
        }
    }
}