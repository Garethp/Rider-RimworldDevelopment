using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AshAndDust.Utils
{
    public class CompRequiresPrecept_Properties: CompProperties
    {
        public List<PreceptDef> OneOf = new ();

        public List<PreceptDef> AllOf = new ();

        public List<PreceptDef> NoneOf = new ();
        
        public CompRequiresPrecept_Properties() =>
            this.compClass = typeof(CompRequiresPrecept);
    }
}