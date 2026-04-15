using AshAndDust.Precepts;
using RimWorld;
using Verse;

namespace AshAndDust.Comps
{
    public class ThoughtOnMemberCorpseDestroyed: PreceptComp, INotify_MemberCorpseDestroyed
    {
        public ThoughtDef thoughtDef;
        
        public void Notify_MemberCorpseDestroyed(Pawn believer, Pawn deadPerson, Precept precept)
        {
            believer.needs?.mood?.thoughts.memories.TryGainMemory(thoughtDef, deadPerson, precept);
        }
    }
}