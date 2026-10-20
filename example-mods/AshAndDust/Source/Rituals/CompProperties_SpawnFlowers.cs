using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AshAndDust.Rituals
{
    public class CompProperties_SpawnFlowersDefaults
    {
        public static int plantSpawnRadius = 5;

        public static int massBurialRadius = 10;
        
        public List<ThingDef> plantsToNotOverwrite => new ()
        {
            DefDatabase<ThingDef>.GetNamed("Plant_TreeGauranlen"),
            DefDatabase<ThingDef>.GetNamed("Plant_PodGauranlen"),
            DefDatabase<ThingDef>.GetNamed("Plant_MossGauranlen"),
            DefDatabase<ThingDef>.GetNamed("Plant_TreeAnima"),
            DefDatabase<ThingDef>.GetNamed("Plant_GrassAnima")
        };

        public List<ThingDef> baseDecorativePlants => new ()
        {
            DefDatabase<ThingDef>.GetNamed("Plant_Dandelion"),
            DefDatabase<ThingDef>.GetNamed("Plant_Astragalus")
        };

        public List<ThingDef> enemyDecorativePlants => new ()
        {
            DefDatabase<ThingDef>.GetNamed("Plant_Clivia"),
            DefDatabase<ThingDef>.GetNamed("Plant_Berry")
        };

        public List<ThingDef> rareRewardPlants => new ()
        {
            DefDatabase<ThingDef>.GetNamed("Plant_Ambrosia")
        };
    }
    
    public class CompProperties_SpawnFlowers : RitualOutcomeComp
    {
        public List<ThingDef> plantsToNotOverwrite;

        public List<ThingDef> baseDecorativePlants;

        public List<ThingDef> enemyDecorativePlants;

        public List<ThingDef> rareRewardPlants;

        public int plantSpawnRadius = 5;

        public int massBurialRadius = 10;

        private SpawnFlowersData data;

        public override RitualOutcomeComp_Data MakeData()
        {
            if (data?.baseDecorativePlants == null || data.enemyDecorativePlants == null || data.rareRewardPlants == null)
            {
                data = new SpawnFlowersData(plantsToNotOverwrite, baseDecorativePlants, enemyDecorativePlants, rareRewardPlants, plantSpawnRadius, massBurialRadius);
            }

            return data;
        }

        public override bool Applies(LordJob_Ritual ritual)
        {
            return true;
        }
    }

    public class SpawnFlowersData : RitualOutcomeComp_Data
    {
        public List<ThingDef> plantsToNotOverwrite;

        public List<ThingDef> baseDecorativePlants;

        public List<ThingDef> enemyDecorativePlants;

        public List<ThingDef> rareRewardPlants;

        public int plantSpawnRadius;

        public int massBurialRadius;

        public SpawnFlowersData()
        {
        }
        
        public SpawnFlowersData(List<ThingDef> plantsToNotOverwrite, List<ThingDef> baseDecorativePlants, List<ThingDef> enemyDecorativePlants, List<ThingDef> rareRewardPlants, int plantSpawnRadius, int massBurialRadius)
        {
            this.plantsToNotOverwrite = plantsToNotOverwrite;
            this.baseDecorativePlants = baseDecorativePlants;
            this.enemyDecorativePlants = enemyDecorativePlants;
            this.rareRewardPlants = rareRewardPlants;
            this.plantSpawnRadius = plantSpawnRadius;
            this.massBurialRadius = massBurialRadius;
        }
        
        // public override void ExposeData()
        // {
        //     Scribe_Values.Look<List<ThingDef>>(ref this.plantsToNotOverwrite, "plantsToNotOverwrite");
        //     Scribe_Values.Look<List<ThingDef>>(ref this.baseDecorativePlants, "baseDecorativePlants");
        //     Scribe_Values.Look<List<ThingDef>>(ref this.enemyDecorativePlants, "enemyDecorativePlants");
        //     Scribe_Values.Look<List<ThingDef>>(ref this.rareRewardPlants, "rareRewardPlants");
        //     Scribe_Values.Look<int>(ref this.plantSpawnRadius, "plantSpawnRadius");
        //     Scribe_Values.Look<int>(ref this.massBurialRadius, "massBurialRadius");
        //
        //     base.ExposeData();
        // }
    }
}