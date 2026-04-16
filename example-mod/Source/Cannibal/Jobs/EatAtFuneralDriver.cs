using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AshAndDust.Cannibal.Jobs
{
    public class EatAtFuneralDriver : JobDriver
  {
    private const TargetIndex GraveIndex = TargetIndex.A;
    private const TargetIndex CellIndex = TargetIndex.B;
    private const int BloodFilthIntervalTick = 40;
    private const float ChanceToProduceBloodFilth = 0.25f;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => this.pawn.ReserveSittableOrSpot(this.job.targetB.Cell, this.job, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
      if (!ModLister.CheckIdeology("Cannibal eat job")) yield break;
      
      this.EndOnDespawnedOrNull(TargetIndex.A);
      yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.OnCell);
      // var totalBuildingNutrition = f.TargetA.Thing.def.CostList.Sum(x => x.thingDef.GetStatValueAbstract(StatDefOf.Nutrition) * x.count);
      var eat = new Toil
      {
        tickIntervalAction = (delta) =>
        {
          pawn.rotationTracker.FaceCell(TargetA.Thing.OccupiedRect().ClosestCellTo(pawn.Position));
          pawn.GainComfortFromCellIfPossible(delta);
          // if (pawn.needs.food != null)
          //   pawn.needs.food.CurLevel += totalBuildingNutrition / pawn.GetLord().ownedPawns.Count / eat.defaultDuration;
          if (!pawn.IsHashIntervalTick(40) || Rand.Value >= 0.25)
            return;
          var pawnCell = Rand.Bool ? pawn.Position : pawn.RandomAdjacentCellCardinal();
          if (!pawnCell.InBounds(pawn.Map))
            return;
          FilthMaker.TryMakeFilth(pawnCell, pawn.Map, ThingDefOf.Human.race.BloodDef);
        }
      };
      
      eat.AddFinishAction(() =>
      {
        if (pawn.mindState != null)
          pawn.mindState.lastHumanMeatIngestedTick = Find.TickManager.TicksGame;
        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteHumanMeat, pawn.Named(HistoryEventArgsNames.Doer)));
      });
      eat.WithEffect(EffecterDefOf.EatMeat, TargetIndex.A);
      eat.PlaySustainerOrSound(SoundDefOf.RawMeat_Eat);
      eat.handlingFacing = true;
      eat.defaultCompleteMode = ToilCompleteMode.Delay;
      eat.defaultDuration = job.doUntilGatheringEnded ? job.expiryInterval : job.def.joyDuration;
      yield return eat;
    }
  }
}