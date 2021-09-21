using HarmonyLib;
using System;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ProperShotguns
{

    static class Patch_Projectile
    {

        [HarmonyPatch(typeof(Projectile))]
        [HarmonyPatch("DamageAmount", MethodType.Getter)]

        public static class Get_DamageAmount
        {

            public static void Postfix(Projectile __instance, ref int __result)
            {
                var verbCache = __instance.TryGetComp<CompProjectileVerbCache>();

                if (ShotgunExtension.Get(__instance.def).pelletCount != 0 && verbCache != null)
                {
                    var shotgunExtension = ShotgunExtension.Get(__instance.def);
                    float adjustedDamage = (float)__result / shotgunExtension.pelletCount;

                    // Determine pellet damage
                    switch (ProperShotgunsSettings.damageRoundMode)
                    {
                        case ShotgunDamageRoundMode.Random:
                            __result = GenMath.RoundRandom(adjustedDamage);
                            return;
                        case ShotgunDamageRoundMode.Standard:
                            __result = Mathf.RoundToInt(adjustedDamage);
                            return;
                        case ShotgunDamageRoundMode.Floor:
                            __result = Mathf.FloorToInt(adjustedDamage);
                            return;
                        case ShotgunDamageRoundMode.Ceil:
                            __result = Mathf.CeilToInt(adjustedDamage);
                            return;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }

        }
        [HarmonyPatch(typeof(Projectile), "Launch", new Type[]
            {
            typeof(Thing),
            typeof(LocalTargetInfo),
            typeof(LocalTargetInfo),
            typeof(ProjectileHitFlags),
            typeof(bool),
            typeof(Thing)
            })]
        public static class Launch
        {
            public static void Postfix(Projectile __instance, Thing launcher)
            {
                var verbCache = __instance.TryGetComp<CompProjectileVerbCache>();
                if (verbCache != null)
                {
                    if (launcher is IAttackTargetSearcher attackTargetSearcher)
                        verbCache.cachedVerbClass = attackTargetSearcher.CurrentEffectiveVerb.verbProps.verbClass;
                    else if (__instance.def.HasModExtension<ShotgunExtension>())
                        verbCache.cachedVerbClass = typeof(Verb_ShootShotgun);
                }
                else
                    Log.Warning($"CompProjectileVerbCache for {__instance} is null.");
            }

        }

    }

}
