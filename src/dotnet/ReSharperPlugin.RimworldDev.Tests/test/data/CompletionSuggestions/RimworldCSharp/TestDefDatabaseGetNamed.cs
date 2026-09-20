using Verse;

namespace MyMod
{
    public static class Lookup
    {
        public static ThingDef Get() => DefDatabase<ThingDef>.GetNamed("{caret}");
    }
}
