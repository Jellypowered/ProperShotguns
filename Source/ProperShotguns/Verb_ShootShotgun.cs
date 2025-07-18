using RimWorld;
using Verse;

namespace ProperShotguns
{
    public class Verb_ShootShotgun : Verb_LaunchProjectile
    {

        protected override int ShotsPerBurst => verbProps.burstShotCount;

        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (currentTarget.Thing is Pawn pawn && !pawn.Downed && !pawn.IsColonyMech && CasterIsPawn && CasterPawn.skills != null)
            {
                float baseExp = pawn.HostileTo(caster) ? SkillTuning.XpPerSecondFiringHostile : SkillTuning.XpPerSecondFiringNonHostile;
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
                for (int i = 0; i < shotgunExtension.pelletCount - 1; i++)
                {
                    base.TryCastShot();
                }
            }

            return castedShot;
        }

    }
}
