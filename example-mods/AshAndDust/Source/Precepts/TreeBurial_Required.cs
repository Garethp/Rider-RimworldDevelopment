using RimWorld;
using Verse;

namespace AshAndDust.Precepts
{
    public class TreeBurial_Required: TreeBurial_WhenPossible, INotify_NonMemberCorpseDestroyed
    {
        public void Notify_NonMemberCorpseDestroyed(Pawn believer, Pawn deadPerson, Precept precept)
        {
            believer.needs?.mood?.thoughts.memories.TryGainMemory(Defs.TreeBurial_EnemyCorpseDestroyed, deadPerson, precept);
        }
    }
}