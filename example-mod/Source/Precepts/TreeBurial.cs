using RimWorld;
using Verse;

namespace AshAndDust.Precepts
{
    public class TreeBurial: Precept, INotify_MemberCorpseDestroyed
    {
        public override void Notify_MemberCorpseDestroyed(Pawn p)
        {
        }

        public void Notify_MemberCorpseDestroyed(Pawn believer, Pawn deadPerson, Precept precept)
        {
            believer.needs?.mood?.thoughts.memories.TryGainMemory(Defs.TreeBurial_EnemyCorpseDestroyed, deadPerson, precept);
        }
    }
}