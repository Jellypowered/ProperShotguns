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

        // Transpiler would be better, but this postfix modifies the list that gets returned
        [HarmonyPatch(typeof(ProjectileProperties))]
        [HarmonyPatch("ExtraDamages", MethodType.Getter)]
        public static class Get_ExtraDamages
        {
            public static void Postfix(ProjectileProperties __instance, ref List<ExtraDamage> __result)
            {
                if (!ProperShotgunsSettings.divideSecondaryEffects)
                    return;

                // Get the projectile def that owns these properties
                ThingDef projectileDef = DefDatabase<ThingDef>.AllDefs.FirstOrDefault(
                    def => def.projectile == __instance
                );

                if (projectileDef != null && ShotgunExtension.Get(projectileDef).pelletCount != 0)
                {
                    var shotgunExtension = ShotgunExtension.Get(projectileDef);

                    // Create a new list with divided damage amounts
                    if (__result != null && __result.Count > 0)
                    {
                        List<ExtraDamage> modifiedDamages = new List<ExtraDamage>();
                        foreach (var extraDamage in __result)
                        {
                            ExtraDamage divided = new ExtraDamage
                            {
                                def = extraDamage.def,
                                amount = Mathf.RoundToInt(extraDamage.amount / (float)shotgunExtension.pelletCount),
                                chance = extraDamage.chance,
                                armorPenetration = extraDamage.armorPenetration / shotgunExtension.pelletCount
                            };
                            modifiedDamages.Add(divided);
                        }
                        __result = modifiedDamages;
                    }
                }
            }
        }
    }
}
