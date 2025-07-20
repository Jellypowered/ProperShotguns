using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ProperShotguns
{
    public static class Patch_Pawn_MindState
    {
        [HarmonyPatch(typeof(Pawn_MindState), "StartFleeingBecauseOfPawnAction")]
        public static class StartFleeingBecauseOfPawnAction
        {
            public static bool Prefix(Pawn_MindState __instance, Thing instigator)
            {
                Pawn pawn = __instance.pawn;

                // Block if the pawn just started a Flee Job this tick
                if (
                    pawn.CurJobDef == JobDefOf.Flee
                    && pawn.CurJob.startTick == Find.TickManager.TicksGame
                )
                {
                    return false;
                }

                return true; // allow original method to run
            }
        }
    }
}
