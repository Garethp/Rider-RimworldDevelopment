using System;
using System.Collections.Generic;
using AshAndDust.Buildings;
using AshAndDust.Plants;
using AshAndDust.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace AshAndDust.Rituals
{
    public class WeightedRandomPlant
    {
        public WeightedRandom<List<ThingDef>> WeightedRandom;
        public List<ThingDef> AllPlants = new();
        private Random r;

        public WeightedRandomPlant(params WeightedParameter<List<ThingDef>>[] parameters)
        {
            WeightedRandom = new WeightedRandom<List<ThingDef>>(parameters);
            
            foreach (var parameter in parameters)
            {
                foreach (var plant in parameter.Item)
                {
                    AllPlants.Add(plant);
                }
            }
            
            r = new Random();
        }

        public ThingDef GetPlant()
        {
            var plants = WeightedRandom.GetRandom();
            return plants[r.Next(plants.Count)];
        }
    }

    public class TreeBurialOutcome : RitualOutcomeEffectWorker_FromQuality
    {
        private Random random = new Random();

        public TreeBurialOutcome()
        {
        }

        public TreeBurialOutcome(RitualOutcomeEffectDef def) : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            base.Apply(progress, totalPresence, jobRitual);
            
            var target = jobRitual.selectedTarget;
            var grave = (Building_TreeGrave) target.Thing;

            var map = grave.Map;
            var position = grave.Position;


            var props = (SpawnFlowersData) compDatas.Find(data => data is SpawnFlowersData);
            if (props == null)
            {
                return;
            }

            var defaults = new CompProperties_SpawnFlowersDefaults();

            var massBurialRadius = props.massBurialRadius;
            var maxRadius = props.plantSpawnRadius;
            var plantsNotToOverwrite = props.plantsToNotOverwrite ?? defaults.plantsToNotOverwrite;

            // 66% chance of this occuring
            var decorationPlants = props.baseDecorativePlants ?? defaults.baseDecorativePlants;

            // 30% chance of this occuring
            var enemyDecorationPlants = props.enemyDecorativePlants ?? defaults.enemyDecorativePlants;

            // 4% chance of this occuring
            var rewardPlants = defaults.rareRewardPlants;

            if (massBurialRadius == 0)
            {
                massBurialRadius = CompProperties_SpawnFlowersDefaults.massBurialRadius;
            }

            if (maxRadius == 0)
            {
                maxRadius = CompProperties_SpawnFlowersDefaults.plantSpawnRadius;
            }
            
            if (grave.Corpse.InnerPawn.Faction == Faction.OfPlayer)
            {
                var weightedPlants =
                    new WeightedRandomPlant(new WeightedParameter<List<ThingDef>>(decorationPlants, 66));

                var speaker = jobRitual.PawnWithRole("moralist");

                var treeThing = ThingMaker.MakeThing(Defs.AshAndDust_Plants_AncestorTree);
                treeThing.StyleDef = speaker.Ideo.GetStyleFor(Defs.AshAndDust_Plants_AncestorTree);
                var tree = (Plants_AncestorTree) GenSpawn.Spawn(treeThing, target.Cell, target.Map);

                tree.TryAcceptThing(grave.Corpse);
                target.Thing.Destroy();
                DoGrowSubplant(map, position, weightedPlants, plantsNotToOverwrite, maxRadius, false);
            }
            else
            {
                var weightedPlants = new WeightedRandomPlant(
                    new WeightedParameter<List<ThingDef>>(decorationPlants, 66),
                    new WeightedParameter<List<ThingDef>>(enemyDecorationPlants, 30),
                    new WeightedParameter<List<ThingDef>>(rewardPlants, 4)
                );

                GetNearbyEnemyGraves(position, map, massBurialRadius).ForEach(enemyGrave =>
                {
                    var currentPosition = enemyGrave.Position;

                    enemyGrave.Corpse.Destroy();
                    enemyGrave.Destroy();

                    DoGrowSubplant(map, currentPosition, weightedPlants, plantsNotToOverwrite, maxRadius, false, true);
                });
            }
        }

        private List<Building_TreeGrave> GetNearbyEnemyGraves(IntVec3 position, Map map, int radius)
        {
            var graves = new List<Building_TreeGrave>();
            var num = GenRadial.NumCellsInRadius(radius);

            for (var radiusIndex = 0; radiusIndex < num; ++radiusIndex)
            {
                var radialPosition = position + GenRadial.RadialPattern[radiusIndex];
                radialPosition
                    .GetThingList(map)
                    .FindAll(thing => thing is Building_TreeGrave {HasCorpse: true} grave
                                      && grave.Corpse.InnerPawn?.RaceProps?.Humanlike == true
                                      && grave.Corpse.InnerPawn.Faction != Faction.OfPlayer
                    )
                    .ForEach(grave =>
                        graves.AddDistinct((Building_TreeGrave) grave)
                    );
            }

            return graves;
        }

        public void DoGrowSubplant(Map map, IntVec3 position, WeightedRandomPlant plants,
            List<ThingDef> plantsToNotOverwrite,
            int maxRadius, bool canSpawnOverPlayerSownPlants, bool includeTargetSpot = false, bool force = false)
        {
            var num = GenRadial.NumCellsInRadius(maxRadius);
            if (includeTargetSpot)
                DoGrowPlantAt(map, plants, plantsToNotOverwrite, canSpawnOverPlayerSownPlants, position, position);

            for (int index1 = 0; index1 < num; ++index1)
            {
                IntVec3 intVec3 = position + GenRadial.RadialPattern[index1];
                DoGrowPlantAt(map, plants, plantsToNotOverwrite, canSpawnOverPlayerSownPlants, position, intVec3);
            }
        }

        private void DoGrowPlantAt(Map map, WeightedRandomPlant plants, List<ThingDef> plantsToNotOverwrite,
            bool canSpawnOverPlayerSownPlants, IntVec3 position, IntVec3 intVec3)
        {
            if (random.Next(100) >= 50) return;

            if (intVec3.InBounds(map) && WanderUtility.InSameRoom(position, intVec3, map))
            {
                bool flag = false;
                List<Thing> thingList = intVec3.GetThingList(map);
                foreach (var thing in thingList)
                {
                    if (plants.AllPlants.Contains(thing.def))
                    {
                        flag = true;
                        break;
                    }

                    if (!plantsToNotOverwrite.NullOrEmpty())
                    {
                        for (int index2 = 0; index2 < plantsToNotOverwrite.Count; ++index2)
                        {
                            if (thing.def == plantsToNotOverwrite[index2])
                            {
                                flag = true;
                                break;
                            }
                        }
                    }
                }

                if (flag) return;

                if (!canSpawnOverPlayerSownPlants)
                {
                    Plant plant = intVec3.GetPlant(map);
                    Zone zone = map.zoneManager.ZoneAt(intVec3);
                    if (plant != null && plant.sown && zone != null && zone is Zone_Growing)
                        return;
                }

                var subplant = plants.GetPlant();
                if (subplant.CanEverPlantAt(intVec3, map, true))
                {
                    for (int index3 = thingList.Count - 1; index3 >= 0; --index3)
                    {
                        if (thingList[index3].def.category == ThingCategory.Plant)
                            thingList[index3].Destroy();
                    }

                    var plant = (Plant) GenSpawn.Spawn(subplant, intVec3, map);
                    plant.Growth = 1f;
                }
            }
        }
    }
}