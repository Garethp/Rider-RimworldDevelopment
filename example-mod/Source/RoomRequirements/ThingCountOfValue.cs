using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AshAndDust.RoomRequirements
{
    public class ThingCountOfValue : RoomRequirement_Thing
    {
        public int value;
        
        public int count;

        public override bool Met(Room room, Pawn pawn = null) => Count(room) >= count;

        public int Count(Room room) => ThingCount(room, thingDef);
        
        public int ThingCount(Room r, ThingDef def)
        {
            return r.ContainedThings(def).ToList().FindAll(thing => thing.MarketValue >= value).Count;
        }

        public override string Label(Room room = null)
        {
            var flag = !labelKey.NullOrEmpty();
            string str = flag ? labelKey.Translate() : thingDef.label;
            
            if (room == null)
            {
                return $"{str} worth at least ${value} x{count}";
            }
            
            return $"{str} worth at least ${value} {Count(room)}/{count}";
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var configError in base.ConfigErrors())
                yield return configError;
            if (count <= 0)
                yield return "count must be larger than 0";
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref count, "count");
            Scribe_Values.Look(ref value, "value");
        }
    }
}