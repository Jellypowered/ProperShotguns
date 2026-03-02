using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace ProperShotguns
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            ProperShotguns.harmonyInstance.PatchAll();

            // Patch Verb_LaunchProjectile for turret shotgun support
            PatchVerbLaunchProjectile();

            // Patch Yayo's Combat ammo consumption if the mod is loaded
            PatchYayoCombatIfPresent();

            // Patch Yayo's Combat Addon ammo consumption if the mod is loaded
            PatchYayoCombatAddonIfPresent();

            // Patch GunControl ammo consumption if the mod is loaded
            PatchGunControlIfPresent();
        }

        private static void PatchVerbLaunchProjectile()
        {
            try
            {
                var verbMethod = AccessTools.Method(typeof(Verb_LaunchProjectile), "TryCastShot");
                if (verbMethod != null)
                {
                    ProperShotguns.harmonyInstance.Patch(
                        verbMethod,
                        postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(VerbLaunchProjectile_TryCastShot_Postfix))
                    );
                    Log.Message("[Proper Shotguns] Patched Verb_LaunchProjectile.TryCastShot for turret support");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Proper Shotguns] Failed to patch Verb_LaunchProjectile: {ex.Message}");
            }
        }

        private static void VerbLaunchProjectile_TryCastShot_Postfix(bool __result, Verb_LaunchProjectile __instance)
        {
            // Only process if the shot was successful and this isn't already Verb_ShootShotgun
            if (!__result || __instance is Verb_ShootShotgun)
                return;

            // Check if we're already firing additional pellets (prevent recursion)
            if (Verb_ShootShotgun.isFiringAdditionalPellets)
                return;

            var shotgunExtension = ShotgunExtension.Get(__instance.verbProps.defaultProjectile);
            if (shotgunExtension.pelletCount > 1)
            {
                // Fire additional pellets for turrets and other non-Verb_ShootShotgun launchers
                Verb_ShootShotgun.isFiringAdditionalPellets = true;
                try
                {
                    // Use reflection to call protected TryCastShot method
                    var tryCastShot = AccessTools.Method(typeof(Verb_LaunchProjectile), "TryCastShot");
                    for (int i = 0; i < shotgunExtension.pelletCount - 1; i++)
                    {
                        tryCastShot?.Invoke(__instance, null);
                    }
                }
                finally
                {
                    Verb_ShootShotgun.isFiringAdditionalPellets = false;
                }
            }
        }

        private static void PatchYayoCombatIfPresent()
        {
            // Check if Yayo's Combat is loaded (various package IDs for different versions)
            bool hasYayoCombat = ModsConfig.IsActive("Yayo.CombatAddon") ||
                ModsConfig.IsActive("yayo.combat3") ||
                ModsConfig.IsActive("Yayo.Combat3") ||
                ModsConfig.IsActive("Mlie.YayosCombat3");

            bool hasYayoAddon = ModsConfig.IsActive("Syrus.YayosCombatAddon");

            if (hasYayoCombat)
            {
                try
                {
                    // Find Yayo's Combat assembly
                    var yayoAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a =>
                        {
                            var name = a.GetName().Name;
                            return name.Contains("Combat") &&
                                   (name.Contains("Yayo") || name.Contains("CombatAddon") || name.Contains("Combat3"));
                        });

                    if (yayoAssembly != null)
                    {
                        string addonNote = hasYayoAddon ? " (with Addon)" : "";
                        Log.Message($"[Proper Shotguns] Found Yayo's Combat assembly: {yayoAssembly.GetName().Name}{addonNote}");

                        // Try to patch common Yayo's Combat ammo classes
                        TryPatchYayoAmmoClass(yayoAssembly, "CompAmmo");
                        TryPatchYayoAmmoClass(yayoAssembly, "CompMagazine");
                        TryPatchYayoAmmoClass(yayoAssembly, "Magazine");
                        TryPatchYayoAmmoClass(yayoAssembly, "AmmoComp");
                        TryPatchYayoAmmoClass(yayoAssembly, "CompAmmoUser");
                    }
                    else
                    {
                        Log.Warning("[Proper Shotguns] Yayo's Combat is active but assembly not found. Ammo consumption fix may not work.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[Proper Shotguns] Error while patching Yayo's Combat: {ex.Message}");
                }
            }
            else if (hasYayoAddon)
            {
                Log.Warning("[Proper Shotguns] Yayo's Combat Addon detected but base mod not found. Addon requires Yayo's Combat 3.");
            }
        }

        private static void PatchYayoCombatAddonIfPresent()
        {
            // Check if Yayo's Combat Addon is loaded
            if (ModsConfig.IsActive("Syrus.YayosCombatAddon"))
            {
                try
                {
                    // Find Yayo's Combat Addon assembly
                    var addonAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a =>
                        {
                            var name = a.GetName().Name;
                            return name.Contains("YayosCombatAddon") || name.Equals("YayosCombatAddon");
                        });

                    if (addonAssembly != null)
                    {
                        Log.Message($"[Proper Shotguns] Found Yayo's Combat Addon assembly: {addonAssembly.GetName().Name}");

                        // Patch the addon's replacement UsedOnce method
                        TryPatchAddonMethod(addonAssembly, "HarmonyPatches", "Patch_CompReloadable_UsedOnce");

                        // Patch the auto-reload utility methods
                        TryPatchAddonReloadUtility(addonAssembly, "ReloadUtility");
                    }
                    else
                    {
                        Log.Warning("[Proper Shotguns] Yayo's Combat Addon is active but assembly not found. Addon ammo fix may not work.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[Proper Shotguns] Error while patching Yayo's Combat Addon: {ex.Message}");
                }
            }
        }

        private static void TryPatchAddonMethod(Assembly assembly, string className, string methodName)
        {
            try
            {
                var type = assembly.GetTypes().FirstOrDefault(t => t.Name == className);
                if (type != null)
                {
                    var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    if (method != null)
                    {
                        try
                        {
                            ProperShotguns.harmonyInstance.Patch(
                                method,
                                prefix: new HarmonyMethod(typeof(Patch_YayoCombat_AmmoConsumption)
                                    .GetMethod(nameof(Patch_YayoCombat_AmmoConsumption.PreventAmmoConsumptionForAdditionalPellets)))
                            );
                            Log.Message($"[Proper Shotguns] Successfully patched Yayo's Combat Addon: {type.Name}.{method.Name}");
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"[Proper Shotguns] Failed to patch Addon method {methodName}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail - class may not exist in this version
            }
        }

        private static void TryPatchAddonReloadUtility(Assembly assembly, string className)
        {
            try
            {
                var type = assembly.GetTypes().FirstOrDefault(t => t.Name == className);
                if (type != null)
                {
                    // Look for reload methods that might trigger ammo consumption
                    var reloadMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                        .Where(m => m.Name.Contains("Reload") && !m.Name.Contains("Reloadable"))
                        .ToList();

                    foreach (var method in reloadMethods)
                    {
                        // We don't need to patch these directly since they call the already-patched methods
                        // But we log them for debugging
                        Log.Message($"[Proper Shotguns] Found Addon reload utility: {type.Name}.{method.Name} (covered by base patches)");
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail - class may not exist in this version
            }
        }

        private static void TryPatchYayoAmmoClass(Assembly assembly, string className)
        {
            try
            {
                var type = assembly.GetTypes().FirstOrDefault(t => t.Name.Contains(className));
                if (type != null)
                {
                    // Look for methods that consume ammo
                    var consumeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                        .Where(m => m.Name.Contains("Consume") ||
                                   m.Name.Contains("Use") ||
                                   m.Name.Contains("Reduce") ||
                                   m.Name.Contains("TryConsum") ||
                                   m.Name.Contains("UsedOnce"))
                        .ToList();

                    foreach (var method in consumeMethods)
                    {
                        try
                        {
                            ProperShotguns.harmonyInstance.Patch(
                                method,
                                prefix: new HarmonyMethod(typeof(Patch_YayoCombat_AmmoConsumption)
                                    .GetMethod(nameof(Patch_YayoCombat_AmmoConsumption.PreventAmmoConsumptionForAdditionalPellets)))
                            );
                            Log.Message($"[Proper Shotguns] Successfully patched Yayo's Combat: {type.Name}.{method.Name}");
                        }
                        catch (Exception)
                        {
                            // Silent fail for individual methods - some may not be patchable
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silent fail - class may not exist in this version
            }
        }

        private static void PatchGunControlIfPresent()
        {
            // Check if GunControl is loaded
            if (ModsConfig.IsActive("Nas.guncontrol"))
            {
                try
                {
                    // Find GunControl assembly
                    var gunControlAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a =>
                        {
                            var name = a.GetName().Name;
                            return name.Contains("GunControl") || name.Contains("guncontrol");
                        });

                    if (gunControlAssembly != null)
                    {
                        Log.Message($"[Proper Shotguns] Found GunControl assembly: {gunControlAssembly.GetName().Name}");

                        // Try to patch GunControl's virtual magazine ammo consumption
                        TryPatchGunControlVirtualMagazine(gunControlAssembly);
                    }
                    else
                    {
                        Log.Warning("[Proper Shotguns] GunControl is active but assembly not found. Ammo consumption fix may not work.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning($"[Proper Shotguns] Error while patching GunControl: {ex.Message}");
                }
            }
        }

        private static void TryPatchGunControlVirtualMagazine(Assembly assembly)
        {
            try
            {
                // Find the Patch_VirtualMagazine_Launch class
                var patchClass = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == "Patch_VirtualMagazine_Launch" ||
                                        t.FullName?.Contains("Patch_VirtualMagazine_Launch") == true);

                if (patchClass != null)
                {
                    // Find the Postfix method that consumes ammo
                    var postfixMethod = patchClass.GetMethod("Postfix",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                    if (postfixMethod != null)
                    {
                        try
                        {
                            ProperShotguns.harmonyInstance.Patch(
                                postfixMethod,
                                prefix: new HarmonyMethod(typeof(Patch_GunControl_AmmoConsumption)
                                    .GetMethod(nameof(Patch_GunControl_AmmoConsumption.PreventVMAmmoConsumptionForAdditionalPellets)))
                            );
                            Log.Message($"[Proper Shotguns] Successfully patched GunControl: {patchClass.Name}.Postfix");
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"[Proper Shotguns] Failed to patch GunControl Postfix: {ex.Message}");
                        }
                    }
                    else
                    {
                        Log.Warning("[Proper Shotguns] Could not find GunControl Postfix method");
                    }
                }
                else
                {
                    Log.Warning("[Proper Shotguns] Could not find GunControl Patch_VirtualMagazine_Launch class");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[Proper Shotguns] Error patching GunControl virtual magazine: {ex.Message}");
            }
        }
    }

    // Harmony patch class for Yayo's Combat compatibility
    public static class Patch_YayoCombat_AmmoConsumption
    {
        // Prefix patch that prevents ammo consumption when firing additional shotgun pellets
        public static bool PreventAmmoConsumptionForAdditionalPellets()
        {
            // If we're currently firing additional pellets, skip ammo consumption
            if (Verb_ShootShotgun.isFiringAdditionalPellets)
            {
                return false; // Skip the original method
            }
            return true; // Run the original method
        }
    }

    // Harmony patch class for GunControl compatibility
    public static class Patch_GunControl_AmmoConsumption
    {
        // Prefix patch that prevents virtual magazine ammo consumption when firing additional shotgun pellets
        public static bool PreventVMAmmoConsumptionForAdditionalPellets()
        {
            // If we're currently firing additional pellets, skip GunControl's ammo consumption
            if (Verb_ShootShotgun.isFiringAdditionalPellets)
            {
                return false; // Skip the original method
            }
            return true; // Run the original method
        }
    }
}
