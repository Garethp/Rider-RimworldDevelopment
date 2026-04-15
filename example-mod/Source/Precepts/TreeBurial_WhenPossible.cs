using RimWorld;
using Verse;

namespace AshAndDust.Precepts
{
    public class TreeBurial_WhenPossible: TreeBurial
    {
        public void Notify_NonMemberDied(Pawn pawn, Pawn deadPawn, Precept source)
        {
            this.ideo.Notify_MemberDied(pawn);
        }
    }
}