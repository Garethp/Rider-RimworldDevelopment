using RimWorld;
using Verse;

namespace AshAndDust.Plants
{
    public class CompGiveThoughtToRelationsOnDestroy: ThingComp
    {
        private CompProperties_GiveThoughtToRelationsOnDestroy Props => (CompProperties_GiveThoughtToRelationsOnDestroy) props;

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (parent is not Plants_AncestorTree tree) return;

            var ancestor = tree.Ancestor;

            if (previousMap == null)
                return;
            
            if (!Props.message.NullOrEmpty())
                Messages.Message(Props.message, new TargetInfo(parent.Position, previousMap), MessageTypeDefOf.NegativeEvent);
            
            foreach (var otherPawn in previousMap.mapPawns.AllPawns)
            {
                if (otherPawn.Faction != ancestor.Faction || otherPawn.Ideo == null) continue;

                var precept = otherPawn.Ideo.GetPrecept(Defs.TreeBurial_Colonists) ??
                              otherPawn.Ideo.GetPrecept(Defs.TreeBurial_WhenPossible) ??
                              otherPawn.Ideo.GetPrecept(Defs.TreeBurial_Required);

                if (precept == null) continue;
                
                var mostImportantRelation = otherPawn.GetMostImportantRelation(ancestor);
                if (mostImportantRelation != null && mostImportantRelation.familyByBloodRelation)
                {
                    otherPawn.needs?.mood?.thoughts.memories.TryGainMemory(Props.closeFamilyThought, ancestor, precept);
                } else if (mostImportantRelation != null && mostImportantRelation == PawnRelationDefOf.Spouse)
                {
                    otherPawn.needs?.mood?.thoughts.memories.TryGainMemory(Props.spouseThought, ancestor, precept);
                } else if (mostImportantRelation != null && mostImportantRelation == PawnRelationDefOf.Lover)
                {
                    otherPawn.needs?.mood?.thoughts.memories.TryGainMemory(Props.loverThought, ancestor, precept);
                } else if (otherPawn.relations.OpinionOf(ancestor) >= 80)
                {
                    otherPawn.needs?.mood?.thoughts.memories.TryGainMemory(Props.bestFriendThought, ancestor, precept);
                } else if (otherPawn.relations.OpinionOf(ancestor) >= 50)
                {
                    otherPawn.needs?.mood?.thoughts.memories.TryGainMemory(Props.friendThought, ancestor, precept);
                }
                else
                {
                    otherPawn.needs?.mood?.thoughts.memories.TryGainMemory(Props.colonistThought, ancestor, precept);
                }
            }

            tree.Corpse.Destroy();
        }
    }
}