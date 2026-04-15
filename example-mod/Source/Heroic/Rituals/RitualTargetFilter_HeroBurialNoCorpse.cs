using RimWorld;
using Verse;

namespace AshAndDust.Heroic.Rituals
{
    public class RitualTargetFilter_HeroBurialNoCorpse: RitualObligationTargetWorker_AnyEmptyGrave
    {
        public RitualTargetFilter_HeroBurialNoCorpse()
        {
        }

        public RitualTargetFilter_HeroBurialNoCorpse(RitualObligationTargetFilterDef def) : base(def)
        {
        }
        
        protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            var baseReport = base.CanUseTargetInternal(target, obligation);
            if (!baseReport.canUse) return baseReport;

            if (target.Thing.GetRoom().Role.defName != "Tomb") return "Must be inside a Tomb";
            
            if (target.Thing.MarketValue < 500)
            {
                return "Not grand enough";
            }

            return true;
        }

        public override string LabelExtraPart(RitualObligation obligation)
        {
            return obligation.targetA.Thing.LabelShort;
        }
    }
}