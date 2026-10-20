using RimWorld;
using Verse;

namespace AshAndDust.Precepts
{
    public interface INotify_MemberCorpseDestroyed
    {
        void Notify_MemberCorpseDestroyed(Pawn believer, Pawn deadPerson, Precept precept);
    }
}