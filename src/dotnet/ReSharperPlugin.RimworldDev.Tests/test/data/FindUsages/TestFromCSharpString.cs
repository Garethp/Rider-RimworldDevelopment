using RimWorld;
using Verse;

namespace MyMod
{
    [DefOf]
    public static class MyThingDefOf
    {
        public static ThingDef SharedThing;
    }

    public static class Lookup
    {
        public static ThingDef Get() => DefDatabase<ThingDef>.GetNamed("Shared{on}Thing");
    }
}
