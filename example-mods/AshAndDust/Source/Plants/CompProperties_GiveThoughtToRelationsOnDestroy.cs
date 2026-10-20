using RimWorld;
using Verse;

namespace AshAndDust.Plants
{
    public class CompProperties_GiveThoughtToRelationsOnDestroy : CompProperties
    {
        public ThoughtDef closeFamilyThought;
        public ThoughtDef spouseThought;
        public ThoughtDef loverThought;
        public ThoughtDef bestFriendThought;
        public ThoughtDef friendThought;
        public ThoughtDef colonistThought;
        
        [MustTranslate] public string message;

        public CompProperties_GiveThoughtToRelationsOnDestroy() =>
            this.compClass = typeof(CompGiveThoughtToRelationsOnDestroy);
    }
}