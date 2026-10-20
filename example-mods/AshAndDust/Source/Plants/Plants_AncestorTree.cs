using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AshAndDust.Plants
{
    public class Plants_AncestorTree : Plant, IThingHolder, IMeditationSideEffect, IManualMeditationCanFocus
    {
        ThingOwner innerContainer;

        private bool contentsKnown;

        private new Graphic styleGraphicInt;

        public Plants_AncestorTree()
        {
            innerContainer = new ThingOwner<Thing>(this, false);
        }

        public Corpse Corpse
        {
            get
            {
                foreach (var thing in innerContainer)
                {
                    if (thing is Corpse corpse)
                        return corpse;
                }

                return null;
            }
        }

        public Pawn Ancestor => Corpse.InnerPawn;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (Faction?.IsPlayer == true)
                contentsKnown = true;
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public void GetChildHolders(List<IThingHolder> outChildren) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

        public virtual bool Accepts(Thing thing) => innerContainer.CanAcceptAnyOf(thing);

        public virtual bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
        {
            if (!Accepts(thing) || thing is not Corpse corpse)
                return false;
            bool flag;
            if (thing.holdingOwner != null)
            {
                thing.holdingOwner.TryTransferToContainer(thing, innerContainer, thing.stackCount);
                flag = true;
            }
            else
                flag = innerContainer.TryAdd(thing);

            if (!flag)
                return false;

            if (corpse.InnerPawn.Faction?.IsPlayer == true)
                contentsKnown = true;

            return true;
        }

        public override string Label
        {
            get
            {
                if (!contentsKnown) return "Tree of an Unknown Hero";
                return "Tree of the beloved " + Ancestor.Name.ToStringShort;
            }
        }

        public void MeditationTick(Pawn pawn)
        {
            Ancestor.skills.skills
                .FindAll(record => record.passion == Passion.Major || record.passion == Passion.Minor)
                .ForEach(skill =>
                {
                    var skillWeighting = 0.5f;
                    if (skill.passion == Passion.Minor)
                    {
                        skillWeighting *= .5f;
                    }

                    switch (skill.Level)
                    {
                        case > 20:
                        case 20:
                            skillWeighting *= 1f;
                            break;
                        case >= 15:
                            skillWeighting *= .75f;
                            break;
                        case >= 10:
                            skillWeighting *= .5f;
                            break;
                        case >= 5:
                            skillWeighting *= .1f;
                            break;
                        default:
                            skillWeighting *= 0f;
                            break;
                    }

                    pawn.skills.Learn(skill.def, 0.018f * skillWeighting);
                });
        }

        public bool CanUse(Pawn pawn)
        {
            return (pawn.Ideo.HasPrecept(Defs.TreeBurial_Colonists)
                    || pawn.Ideo.HasPrecept(Defs.TreeBurial_WhenPossible)
                    || pawn.Ideo.HasPrecept(Defs.TreeBurial_Required))
                   && Ancestor.IsColonist && pawn.IsColonist;
        }

        public override void ExposeData()
        {
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Values.Look(ref contentsKnown, "contentsKnown");

            base.ExposeData();
        }

        public override Graphic Graphic
        {
            get
            {
                ThingStyleDef styleDef = StyleDef;
                if (styleDef == null || styleDef.Graphic == null)
                    return DefaultGraphic;
                if (styleGraphicInt == null)
                {
                    styleGraphicInt = styleDef.graphicData == null
                        ? styleDef.Graphic
                        : styleDef.graphicData.GraphicColoredFor(this);

                    if (styleGraphicInt.MatSingle.shader != ShaderDatabase.CutoutPlant)
                    {
                        styleGraphicInt.MatSingle.shader = ShaderDatabase.CutoutPlant;
                        WindManager.Notify_PlantMaterialCreated(styleGraphicInt.MatSingle);
                    }
                }

                return styleGraphicInt;
            }
        }
    }
}