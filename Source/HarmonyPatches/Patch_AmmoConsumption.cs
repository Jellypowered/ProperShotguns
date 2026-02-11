using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using Verse;

namespace ProperShotguns
{
    // Patch to prevent ammo consumption for additional shotgun pellets
    [StaticConstructorOnStartup]
    public static class Patch_AmmoConsumption
    {
        static Patch_AmmoConsumption()
        {
            // Try to patch CompReloadable if it exists (vanilla or biotech)
            TryPatchVanillaReloadable();
        }

        private static void TryPatchVanillaReloadable()
        {
            try
            {
                // Look for CompReloadable in RimWorld assemblies
                var compReloadableType = GenTypes.AllTypes
                    .FirstOrDefault(t => t.Name == "CompReloadable" && t.Namespace == "RimWorld");

                if (compReloadableType != null)
                {
                    var usedOnceMethod = compReloadableType.GetMethod("UsedOnce",
                        BindingFlags.Public | BindingFlags.Instance);

                    if (usedOnceMethod != null)
                    {
                        ProperShotguns.harmonyInstance.Patch(
                            usedOnceMethod,
                            prefix: new HarmonyMethod(typeof(Patch_AmmoConsumption)
                                .GetMethod(nameof(PreventVanillaAmmoConsumption)))
                        );
                        Log.Message("[Proper Shotguns] Successfully patched vanilla CompReloadable.UsedOnce");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[Proper Shotguns] Could not patch vanilla CompReloadable: {ex.Message}");
            }
        }

        public static bool PreventVanillaAmmoConsumption()
        {
            // If we're firing additional pellets, don't consume ammo
            if (Verb_ShootShotgun.isFiringAdditionalPellets)
            {
                return false; // Skip the original method
            }
            return true; // Run the original method
        }
    }
}
