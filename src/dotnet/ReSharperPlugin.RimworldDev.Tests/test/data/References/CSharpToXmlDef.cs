using RimWorld;
using Verse;

namespace MyMod
{
    [DefOf]
    public static class MyThingDefOf
    {
        public static ThingDef ModThingA;
        public static ThingDef MissingThing;
        public static SoundDef ModSound;
    }

    public static class Lookup
    {
        public static ThingDef Found() => DefDatabase<ThingDef>.GetNamed("ModThingB");
        public static ThingDef Missing() => DefDatabase<ThingDef>.GetNamed("NoSuchThing");
        public static ThingDef WrongType() => DefDatabase<ThingDef>.GetNamed("ModSound");
        public static SoundDef Sound() => DefDatabase<SoundDef>.GetNamedSilentFail("ModSound");
    }
}
