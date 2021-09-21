using System.Collections.Generic;
using Verse;

namespace ProperShotguns
{

    [StaticConstructorOnStartup]
    public static class StartupPatches
    {
        static StartupPatches()
        {
            List<ThingDef> thingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            //var thingDefs = DefDatabase<ThingDef>.AllDefsListForReading;  //old method. Probably was fine as it. 
            for (int i = 0; i < thingDefs.Count; i++)
            {
                var tDef = thingDefs[i];

                // Add verb caches to all projectile defs
                if (typeof(Projectile).IsAssignableFrom(tDef.thingClass))

                {
                    if (tDef.comps == null)
                        tDef.comps = new List<CompProperties>();
                    tDef.comps.Add(new CompProperties(typeof(CompProjectileVerbCache))); 
                    //Doesn't seem to actually ever add these. Or my check in Patch_Projectile.cs is just wrong.
                }
            }
        }
    }

}
