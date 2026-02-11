using RimWorld;
using Verse;

namespace ProperShotguns
{
    public class Verb_ShootShotgun : Verb_LaunchProjectile
    {
        // Flag to track when we're firing additional pellets (not the first shot)
        // This prevents ammo consumption for additional pellets in mods like Yayo's Combat
        public static bool isFiringAdditionalPellets = false;

        protected override int ShotsPerBurst => verbProps.burstShotCount;

        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (
                currentTarget.Thing is Pawn pawn
                && !pawn.Downed
                && !pawn.IsColonyMech
                && CasterIsPawn
                && CasterPawn.skills != null
            )
            {
                float baseExp = pawn.HostileTo(caster)
                    ? SkillTuning.XpPerSecondFiringHostile
                    : SkillTuning.XpPerSecondFiringNonHostile;
                float cycleTime = verbProps.AdjustedFullCycleTime(this, CasterPawn);
                CasterPawn.skills.Learn(SkillDefOf.Shooting, baseExp * cycleTime, false);
            }
        }

        protected override bool TryCastShot()
        {
            bool castedShot = base.TryCastShot();
            if (castedShot && CasterIsPawn)
                CasterPawn.records.Increment(RecordDefOf.ShotsFired);

            var shotgunExtension = ShotgunExtension.Get(verbProps.defaultProjectile);
            if (castedShot && shotgunExtension.pelletCount - 1 > 0)
            {
                // Set flag before firing additional pellets
                isFiringAdditionalPellets = true;
                try
                {
                    for (int i = 0; i < shotgunExtension.pelletCount - 1; i++)
                    {
                        base.TryCastShot();
                    }
                }
                finally
                {
                    // Always reset flag after firing additional pellets
                    isFiringAdditionalPellets = false;
                }
            }

            return castedShot;
        }
    }
}
