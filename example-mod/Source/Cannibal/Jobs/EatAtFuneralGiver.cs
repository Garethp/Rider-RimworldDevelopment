using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace AshAndDust.Cannibal.Jobs
{
    public class EatAtFuneralGiver : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            var lordJob = pawn.GetLord().LordJob as LordJob_Ritual;
            if (!GatheringsUtility.TryFindRandomCellAroundTarget(pawn, lordJob.selectedTarget.Thing, out var result) && !GatheringsUtility.TryFindRandomCellInGatheringArea(pawn, CellValid, out result))
                return null;
            var job = JobMaker.MakeJob(Defs.EatAtCannibalFuneral, (LocalTargetInfo) lordJob.selectedTarget.Thing, result);
            job.doUntilGatheringEnded = true;
            job.expiryInterval = lordJob == null ? 2000 : lordJob.DurationTicks;
            return job;

            bool CellValid(IntVec3 c)
            {
                foreach (var target in GenRadial.RadialCellsAround(c, 1f, true))
                {
                    if (!pawn.CanReserveAndReach(target, PathEndMode.OnCell, pawn.NormalMaxDanger()))
                        return false;
                }
                return true;
            }
        }
    }
}