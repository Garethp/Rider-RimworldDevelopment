using System.Collections.Generic;
using System.Linq;
using AshAndDust.Buildings;
using RimWorld;
using Verse;

namespace AshAndDust.Rituals
{
    public class PirateBurialOutcome: RitualOutcomeEffectWorker_FromQuality
    {
        public PirateBurialOutcome()
        {
        }

        public PirateBurialOutcome(RitualOutcomeEffectDef def) : base(def)
        {
        }
        
        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            base.Apply(progress, totalPresence, jobRitual);
            
            var target = jobRitual.selectedTarget;
            var grave = (Building_Grave) target.Thing;
            
            if (grave.Corpse != null)
            {
                if (PawnUtility.IsFactionLeader(grave.Corpse.InnerPawn) && grave.Corpse.InnerPawn.Faction == Faction.OfPlayer) grave.Corpse.InnerPawn.Faction.leader = null;
                grave.Corpse.InnerPawn.ideo = null;
                grave.Corpse.Destroy();
            }
            
            foreach (var pawn in jobRitual.PawnsToCountTowardsPresence.ToList())
            {
                var hediff = HediffMaker.MakeHediff(HediffDefOf.AlcoholHigh, pawn);
                var effect = .3f;
                effect /= pawn.BodySize;
                
                AddictionUtility.ModifyChemicalEffectForToleranceAndBodySize(pawn, ChemicalDefOf.Alcohol, ref effect, applyGeneToleranceFactor: true);
                hediff.Severity = effect;
                pawn.health.AddHediff(hediff);
            }

            if (target.Thing is ShuttleCasket shuttle)
            {
                shuttle.Yeet();
            }
            else
            {
                target.Thing.Destroy();
            }
        }
    }
}