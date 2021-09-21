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
                Log.Message("We are in Proper Shotgun's Projectile Postfix");
                var verbCache = __instance.TryGetComp<CompProjectileVerbCache>();
                Log.Message("Looks like verbCache is: " + verbCache); // returns coordinates? 
                /* Example Log Message: Looks like verbCache is: CompProjectileVerbCache(parent=Bullet_Shotgun18306 at=(43, 0, 70)) */

                /* This Shitbox never passes the check below, so it always goes to the "else" Log Message. This is probably my main issue :( */
                if (verbCache != null //We know it isn't null from the above log. 
                                      //I'm guessing the following two lines don't pass.
                   && verbCache.cachedVerbClass is Type t 
                   && t.IsAssignableFrom(typeof(Verb_ShootShotgun))) 
                {
                    var shotgunExtension = ShotgunExtension.Get(__instance.def);
                    float adjustedDamage = (float)__result / shotgunExtension.pelletCount;
                    Log.Message("" + __instance.DamageAmount + "is the current Damage Amount."); //just logging to try and verify it goes here
                    Log.Message("Adjusted damage is: " + adjustedDamage); // More logs. (remove these later) 

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
                else
                    Log.Message($"CachedVerbCache for {__instance} is null.");
            }

        }
        /* 
        public void Launch(
        Thing launcher,
        LocalTargetInfo usedTarget,
        LocalTargetInfo intendedTarget,
        ProjectileHitFlags hitFlags,
        bool preventFriendlyFire = false,
        Thing equipment = null)
        */
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
