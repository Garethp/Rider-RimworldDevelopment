using Verse;

namespace AshAndDust.Comps
{
    public abstract class MeditationSideEffectComp: ThingComp, IMeditationSideEffect
    {
        public abstract void MeditationTick(Pawn pawn);
    }
}