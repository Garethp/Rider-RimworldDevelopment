using RimWorld;

namespace AshAndDust.Cannibal.Rituals
{
    public class RitualObligationTrigger_CannibalFeastProperties: RitualObligationTriggerProperties
    {
        public RitualObligationTrigger_CannibalFeastProperties() => this.triggerClass = typeof (RitualObligationTrigger_CannibalFeast);
    }
}