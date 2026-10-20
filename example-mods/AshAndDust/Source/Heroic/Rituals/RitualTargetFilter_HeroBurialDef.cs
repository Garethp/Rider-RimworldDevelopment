using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AshAndDust.Heroic.Rituals
{
    public class TombRequirement
    {
        public ExpectationDef expectation;

        public List<RoomRequirement> requirements;

        public int tombValue = 0;
    }
    
    public class RitualTargetFilter_HeroBurialDef: RitualObligationTargetFilterDef
    {
        public List<TombRequirement> tombRequirements = new ();

        public List<RoomRequirement> GetCurrentRequirement(Pawn pawn)
        {
            var currentRequirement = ExpectationsUtility.CurrentExpectationFor(pawn);
            
            var requirements =  tombRequirements.Find(tombRequirement => tombRequirement.expectation == currentRequirement) ??
                   tombRequirements[0];

            return requirements != null ? requirements.requirements : new List<RoomRequirement>();
        }
    }
}