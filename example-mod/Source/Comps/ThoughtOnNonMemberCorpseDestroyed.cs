using RimWorld;
using Verse;

namespace AshAndDust.Comps
{
    public class ThoughtOnNonMemberCorpseDestroyed: PreceptComp, INotify_NonMemberCorpseDestroyed
    {
        public ThoughtDef thoughtDef;
        
        public void Notify_NonMemberCorpseDestroyed(Pawn believer, Pawn deadPerson, Precept precept)
        {
            believer.needs?.mood?.thoughts.memories.TryGainMemory(thoughtDef, deadPerson, precept);
        }
    }
}