using AshAndDust.Comps;
using RimWorld;
using Verse;

namespace AshAndDust.Heroic.Comps
{
    public class CompHeroicTomb : MeditationSideEffectComp, IMeditationSideEffect, IManualMeditationCanFocus
    {
        private CompHeroicTomb_Properties Props => (CompHeroicTomb_Properties) props;

        public bool CanUse(Pawn pawn)
        {
            if (parent is not Building_CorpseCasket {HasCorpse: true} casket) return false;

            return pawn.Ideo.HasPrecept(Defs.HeroFuneral) && casket.Corpse.InnerPawn.IsColonist && pawn.IsColonist;
        }

        public override void MeditationTick(Pawn pawn)
        {
            pawn.health.AddHediff(Defs.AshAndDust_ProtectedByHero);
        }
    }
}