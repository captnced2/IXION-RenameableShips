using BulwarkStudios.Stanford.Torus.UI;
using HarmonyLib;

namespace RenameableShips;

public class Patches
{
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

    [HarmonyPatch(typeof(UIWindowBuildingDockingBaySinglePlatform),
        nameof(UIWindowBuildingDockingBaySinglePlatform.SetSpaceVehicle))]
    public static class DockingBaySinglePlatformPatcher
    {
        public static void Postfix(UIWindowBuildingDockingBaySinglePlatform __instance)
        {
            RenamableShipsManager.setRenameButtonVisibility(__instance);
        }
    }
}