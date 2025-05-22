using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BulwarkStudios.Stanford.Torus.UI;
using HarmonyLib;
using IMHelper;
using TMPro;
using UnityEngine;

namespace RenameableShips;

[BepInPlugin(Guid, Name, Version)]
[BepInProcess("IXION.exe")]
[BepInDependency("captnced.IMHelper")]
public class Plugin : BasePlugin
{
    private const string Guid = "captnced.RenameableShips";
    private const string Name = "RenameableShips";
    private const string Version = "2.1.0";
    internal new static ManualLogSource Log;
    private static Harmony harmony;
    private static bool enabled;

    public override void Load()
    {
        Log = base.Log;
        harmony = new Harmony(Guid);
        if (IL2CPPChainloader.Instance.Plugins.ContainsKey("captnced.IMHelper")) enabled = ModsMenu.isSelfEnabled();
        if (!enabled)
            Log.LogInfo("Disabled by IMHelper!");
        else
            init();
    }

    private static void init()
    {
        harmony.PatchAll();
        foreach (var patch in harmony.GetPatchedMethods())
            Log.LogInfo("Patched " + patch.DeclaringType + ":" + patch.Name);
        Log.LogInfo("Loaded \"" + Name + "\" version " + Version + "!");
    }

    private static void disable()
    {
        harmony.UnpatchSelf();
        Log.LogInfo("Unloaded \"" + Name + "\" version " + Version + "!");
    }
    
    public static void enable(bool value)
    {
        enabled = value;
        if (enabled) init();
        else disable();
    }
}

internal static class RenamableShipsManager
{
    private static Transform currentPlatform;
    private static readonly List<ButtonHelper.Button> buttons = [];

    internal static void AddButtons(Transform shipPlatformDisplay)
    {
        var ships = new List<UIWindowBuildingDockingBaySinglePlatform>();
        ships.AddRange(shipPlatformDisplay.GetComponentsInChildren<UIWindowBuildingDockingBaySinglePlatform>());
        foreach (var ship in ships)
            if (!ship.transform.FindChild("Buttons").FindChild("Rename"))
            {
                var renameButton = new ButtonHelper.IconButton("Rename", ship.unassignButton.transform,
                    ship.unassignButton.transform.parent, TriggerRename,
                    typeof(Plugin).Assembly.GetManifestResourceStream("RenameableShips.assets.Rename.png"), "Rename");
                renameButton.createButton();
                renameButton.buttonTransform.parent.localPosition = new Vector3(-14, -35, 0);
                renameButton.buttonTransform.SetAsLastSibling();
                buttons.Add(renameButton);
            }
    }

    internal static void setRenameButtonVisibility(UIWindowBuildingDockingBaySinglePlatform platform)
    {
        var rename = platform.transform.FindChild("Buttons").FindChild("Rename");
        if (!rename == false)
        {
            if (platform.GetComponent<UIWindowBuildingDockingBaySinglePlatform>().spaceVehicleInstance != null)
                rename.gameObject.SetActive(true);
            else
                rename.gameObject.SetActive(false);
        }
    }

    internal static void TriggerRename(Transform platform)
    {
        currentPlatform = platform.parent.parent;
        var shipName = currentPlatform.GetComponent<UIWindowBuildingDockingBaySinglePlatform>().spaceVehicleInstance
            .state.GetTranslatedName();
        PopupHelper.openTextPopup("Rename Ship", shipName, RenameShip, true);
    }

    internal static void RenameShip(string name)
    {
        var shipState = currentPlatform.GetComponent<UIWindowBuildingDockingBaySinglePlatform>().spaceVehicleInstance
            .state;
        var oldName = shipState.GetTranslatedName();
        shipState.shipUniqueName = "captnced/" + name;
        currentPlatform.FindChild("Label").FindChild("Name").GetComponent<TextMeshProUGUI>().text = name;
        Plugin.Log.LogInfo("Renamed ship \"" + oldName + "\" to \"" + name + "\"");
    }
}