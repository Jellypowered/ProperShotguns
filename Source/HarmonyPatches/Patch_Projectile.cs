using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
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

        [HarmonyPatch(
            typeof(Projectile),
            "Launch",
            new Type[]
            {
                typeof(Thing),
                typeof(LocalTargetInfo),
                typeof(LocalTargetInfo),
                typeof(ProjectileHitFlags),
                typeof(bool),
                typeof(Thing),
            }
        )]
        public static class Launch
        {
            public static void Postfix(Projectile __instance, Thing launcher)
            {
                var verbCache = __instance.TryGetComp<CompProjectileVerbCache>();
                if (verbCache != null)
                {
                    if (launcher is IAttackTargetSearcher attackTargetSearcher)
                        verbCache.cachedVerbClass = attackTargetSearcher
                            .CurrentEffectiveVerb
                            .verbProps
                            .verbClass;
                    else if (__instance.def.HasModExtension<ShotgunExtension>())
                        verbCache.cachedVerbClass = typeof(Verb_ShootShotgun);
                }
                else
                    Log.Warning($"CompProjectileVerbCache for {__instance} is null.");
            }
        }

        [HarmonyPatch(typeof(Projectile))]
        [HarmonyPatch("ArmorPenetration", MethodType.Getter)]
        public static class Get_ArmorPenetration
        {
            public static void Postfix(Projectile __instance, ref float __result)
            {
                if (!ProperShotgunsSettings.divideSecondaryEffects)
                    return;

                var verbCache = __instance.TryGetComp<CompProjectileVerbCache>();

                if (ShotgunExtension.Get(__instance.def).pelletCount != 0 && verbCache != null)
                {
                    var shotgunExtension = ShotgunExtension.Get(__instance.def);
                    __result = __result / shotgunExtension.pelletCount;
                }
            }
        }

        [HarmonyPatch(typeof(Projectile))]
        [HarmonyPatch("ExtraDamages", MethodType.Getter)]
        public static class Get_ExtraDamages
        {
            public static void Postfix(Projectile __instance, ref IEnumerable<ExtraDamage> __result)
            {
                if (!ProperShotgunsSettings.divideSecondaryEffects)
                    return;

                var verbCache = __instance.TryGetComp<CompProjectileVerbCache>();

                if (ShotgunExtension.Get(__instance.def).pelletCount != 0 && verbCache != null)
                {
                    var shotgunExtension = ShotgunExtension.Get(__instance.def);

                    // Divide extra damage amounts and armor pen by pellet count
                    __result = __result.Select(ed => new ExtraDamage
                    {
                        def = ed.def,
                        amount = Mathf.Max(1, Mathf.RoundToInt(ed.amount / (float)shotgunExtension.pelletCount)),
                        chance = ed.chance,
                        armorPenetration = ed.armorPenetration / shotgunExtension.pelletCount
                    }).ToList();
                }
            }
        }
    }
}