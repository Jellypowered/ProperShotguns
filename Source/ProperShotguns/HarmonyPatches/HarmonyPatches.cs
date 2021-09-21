using Verse;

namespace ProperShotguns
{

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {

        static HarmonyPatches()
        {
            ProperShotguns.harmonyInstance.PatchAll();
        }

    }

}
