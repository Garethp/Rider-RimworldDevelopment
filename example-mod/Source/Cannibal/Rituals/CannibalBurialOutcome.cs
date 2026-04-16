using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AshAndDust.Cannibal.Rituals
{
    public class CannibalBurialOutcome : RitualOutcomeEffectWorker_FromQuality
    {
        private float nutritionPerTick = -1;
        
        public CannibalBurialOutcome()
        {
        }

        public CannibalBurialOutcome(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override void Tick(LordJob_Ritual ritual, float progressAmount = 1)
        {
            var grave = (Building_CorpseCasket) ritual.selectedTarget.Thing;
            
            var pawns = ritual.PawnsToCountTowardsPresence.ToList();

            if (nutritionPerTick < 0)
            {
                nutritionPerTick = grave.Corpse.GetStatValue(StatDefOf.Nutrition) / (float) pawns.Count() / (float) ritual.DurationTicks;
            }
            
            foreach (var pawn in pawns)
            {
                if (pawn.needs.food != null)
                {
                    pawn.needs.food.CurLevel += nutritionPerTick;
                }
            }
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            base.Apply(progress, totalPresence, jobRitual);

            var grave = (Building_CorpseCasket) jobRitual.selectedTarget.Thing;
            var eaten = grave.Corpse.InnerPawn;

            var skillsToLearn = new List<(SkillDef, float)>();

            eaten.skills.skills
                .FindAll(record => record.passion == Passion.Major || record.passion == Passion.Minor)
                .ForEach(skill =>
                {
                    var skillWeighting = 0.5f;
                    if (skill.passion == Passion.Minor)
                    {
                        skillWeighting *= .5f;
                    }

                    switch (skill.Level)
                    {
                        case > 20:
                        case 20:
                            skillWeighting *= 1f;
                            break;
                        case >= 15:
                            skillWeighting *= .75f;
                            break;
                        case >= 10:
                            skillWeighting *= .5f;
                            break;
                        case >= 5:
                            skillWeighting *= .1f;
                            break;
                        default:
                            skillWeighting *= 0f;
                            break;
                    }

                    skillsToLearn.Add((skill.def, 1000 * skillWeighting));
                });

            foreach (var pawn in jobRitual.PawnsToCountTowardsPresence.ToList())
            {
                skillsToLearn.ForEach(toLearn => { pawn.skills.Learn(toLearn.Item1, toLearn.Item2); });
            }

            var deadPawn = grave.Corpse.InnerPawn;
            if (PawnUtility.IsFactionLeader(deadPawn)) deadPawn.Faction.leader = null;
            grave.Corpse.InnerPawn.ideo = null;
            grave.Corpse.Destroy();
        }
    }
}