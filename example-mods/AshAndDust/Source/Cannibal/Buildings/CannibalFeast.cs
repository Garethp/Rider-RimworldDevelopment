using RimWorld;
using UnityEngine;
using Verse;

namespace AshAndDust.Cannibal.Buildings
{
    public class CannibalFeast: Building_Grave
    {
        public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
        {
            base.DynamicDrawPhaseAt(phase, drawLoc, flip);
            
            if (!HasCorpse) return;
            
            var rotation2 = Rotation;
            if (rotation2 == Rot4.North || rotation2 == Rot4.South)
                drawLoc.z += 0.4f;
            else
                drawLoc.z += 0.2f;

            drawLoc.y += 1;
            
            Corpse.InnerPawn.Drawer.renderer.RenderPawnAt(drawLoc, Rotation, true);
        }
    }
}