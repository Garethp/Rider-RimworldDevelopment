using RimWorld;
using Verse;

namespace AshAndDust
{
    public interface INotify_NonMemberCorpseDestroyed
    {
        void Notify_NonMemberCorpseDestroyed(Pawn believer, Pawn deadPerson, Precept precept);
    }
}