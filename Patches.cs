using BulwarkStudios.Stanford.Torus.UI;
using HarmonyLib;
using I2.Loc;

namespace RenameableShips;

public class Patches
{
    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetTranslation))]
    public static class LocalizationPatch
    {
        public static void Postfix(ref string __result, object[] __args)
        {
            if (__args[0] != null && __args[0].ToString().Contains("captnced/"))
                __result = __args[0].ToString().Replace("captnced/", "");
        }
    }

    [HarmonyPatch(typeof(UIWindowBuilding), nameof(UIWindowBuilding.BeforeOpen))]
    public static class UIWindowBuildingPatcher
    {
        public static void Postfix(UIWindowBuilding __instance)
        {
            if (__instance.transform.name.Equals("UI Window Building Docking Bay"))
            {
                var shipDisplay =
                    __instance.transform.GetComponentInChildren<UIWindowBuildingDockingBayDisplayShipsForPlatforms>()
                        .transform;
                RenamableShipsManager.AddButtons(shipDisplay);
            }
        }
    }
}