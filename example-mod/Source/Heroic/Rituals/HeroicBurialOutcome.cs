using System;
using System.Collections.Generic;
using AshAndDust.Heroic.Comps;
using RimWorld;
using Verse;

namespace AshAndDust.Heroic.Rituals
{
    public class HeroicBurialOutcome : RitualOutcomeEffectWorker_FromQuality
    {
        public HeroicBurialOutcome()
        {
        }

        public HeroicBurialOutcome(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            base.Apply(progress, totalPresence, jobRitual);
            
            var tomb = (Building_CorpseCasket) jobRitual.selectedTarget.Thing;

            var compProps = new CompHeroicTomb_Properties();
            var thingComp = (ThingComp) Activator.CreateInstance(compProps.compClass);
            thingComp.parent = tomb;
            tomb.AllComps.Add(thingComp);
            thingComp.Initialize(compProps);
            
            tomb.def.comps.Add(compProps);
        }
    }
}