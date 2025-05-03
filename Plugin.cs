using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BulwarkStudios.GameSystems.Ui;
using BulwarkStudios.Stanford.Core.UI;
using BulwarkStudios.Stanford.Torus.UI;
using BulwarkStudios.Utils.UI;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using Stanford.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RenameableShips;

[BepInPlugin(Guid, Name, Version)]
[BepInProcess("IXION.exe")]
public class Plugin : BasePlugin
{
    private const string Guid = "captnced.RenameableShips";
    private const string Name = "RenameableShips";
    private const string Version = "1.0.1";
    internal new static ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;
        var harmony = new Harmony(Guid);
        harmony.PatchAll();
        foreach (var patch in harmony.GetPatchedMethods())
            Log.LogInfo("Patched " + patch.DeclaringType + ":" + patch.Name);
        Log.LogInfo("Loaded \"" + Name + "\" version " + Version + "!");
    }
}

internal class RenamableShipsManager
{
    private static Transform renameShipDialogue;
    private static Transform renameShipWindow;
    private static Transform currentPlatform;
    private static Transform clickBlocker;
    private static GameInputLockHandle lockHandle;

    internal static void AddButtons(Transform shipPlatformDisplay)
    {
        SetupRenameShipDialogue();
        var ships = new List<UIWindowBuildingDockingBaySinglePlatform>();
        ships.AddRange(shipPlatformDisplay.GetComponentsInChildren<UIWindowBuildingDockingBaySinglePlatform>());
        foreach (var ship in ships)
            if (!ship.transform.FindChild("Buttons").FindChild("Rename"))
            {
                if (ship.unassignButton.transform.parent.childCount > 2) return;
                var newButton = Object.Instantiate(ship.unassignButton, ship.unassignButton.transform.parent);
                newButton.name = "Rename";
                newButton.transform.SetAsLastSibling();
                newButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
                newButton.GetComponent<UITooltipHoverHelper>().translatedText = "Rename";
                Action buttonClicked = delegate { TriggerRename(newButton.transform.parent.parent); };
                newButton.GetComponent<UiButton>().add_OnTriggered(buttonClicked);
                newButton.transform.parent.localPosition = new Vector3(-14, -35, 0);
                loadButtonTexture(newButton);
            }
    }

    internal static void showRenameButton(UIWindowBuildingDockingBaySinglePlatform platform)
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

    internal static void loadButtonTexture(UiButton button)
    {
        var stream = typeof(Plugin).Assembly.GetManifestResourceStream("RenameableShips.assets.Rename.png");
        if (stream == null) return;
        var oldSprite = button.transform.FindChild("Icon").GetComponent<Image>().sprite;
        var texture = new Texture2D(2, 2);
        texture.LoadImage(stream.ReadBytes());
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), oldSprite.textureRectOffset);
        button.transform.FindChild("Icon").GetComponent<Image>().sprite = sprite;
    }

    internal static void TriggerRename(Transform platform)
    {
        currentPlatform = platform;
        var inputField = renameShipDialogue.FindChild("Content").FindChild("InputField").GetComponent<TMP_InputField>();
        inputField.text = currentPlatform.GetComponent<UIWindowBuildingDockingBaySinglePlatform>().spaceVehicleInstance
            .state.GetTranslatedName();
        inputField.SelectAll();
        renameShipWindow.gameObject.SetActive(true);
        clickBlocker.gameObject.SetActive(true);
        lockHandle = GameInputLockAll.CreateLock();
    }

    internal static void RenameShip()
    {
        var newName = renameShipDialogue.FindChild("Content").FindChild("InputField").GetComponent<TMP_InputField>()
            .text;
        currentPlatform.GetComponent<UIWindowBuildingDockingBaySinglePlatform>().spaceVehicleInstance.state
            .shipUniqueName = "captnced/" + newName;
        currentPlatform.FindChild("Label").FindChild("Name").GetComponent<TextMeshProUGUI>().text = newName;
        CloseRenameShipDialogue();
    }

    internal static void TriggeredClickBlocker()
    {
        CloseRenameShipDialogue();
    }

    private static void SetupRenameShipDialogue()
    {
        if (renameShipDialogue == null)
        {
            clickBlocker = GameObject.Find("Canvas/WindowManagerCenterOption/BackgroundUiBlocker").transform;
            Action clickBlockerClicked = delegate { TriggeredClickBlocker(); };
            clickBlocker.GetComponent<UIWindowClickBlocker>().add_OnClicked(clickBlockerClicked);
            renameShipWindow =
                Object.Instantiate(GameObject.Find("Canvas/WindowManagerCenterOption/UI Window Save Game").transform,
                    GameObject.Find("Canvas").transform);
            renameShipWindow.name = "RenameShipWindow";
            renameShipWindow.gameObject.SetActive(false);
            renameShipWindow.FindChild("Container").gameObject.SetActive(false);
            renameShipWindow.GetComponent<Canvas>().enabled = true;
            renameShipWindow.GetComponent<GraphicRaycaster>().enabled = true;
            renameShipDialogue = renameShipWindow.FindChild("NewSavePopup/Container");
            renameShipDialogue.name = "RenameShipDialogue";
            renameShipDialogue.FindChild("Content").FindChild("InputField").GetComponent<TMP_InputField>()
                .m_TextComponent.m_fontStyle = FontStyles.Normal;
            var header = renameShipDialogue.FindChild("UI Window Header");
            header.FindChild("Title").GetComponent<TextMeshProUGUI>().text = "Rename Ship";
            var headerButton = header.FindChild("Close Button");
            headerButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
            Action closeRenameShipDialogue = delegate { CloseRenameShipDialogue(); };
            headerButton.GetComponent<UiButton>().add_OnTriggered(closeRenameShipDialogue);
            var buttons = renameShipDialogue.FindChild("ButtonContainer");
            var cancelButton = buttons.FindChild("Cancel");
            cancelButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
            cancelButton.GetComponent<UiButton>().add_OnTriggered(closeRenameShipDialogue);
            var okButton = buttons.FindChild("Validate");
            okButton.GetComponent<UiButtonTriggerUnityEvent>().enabled = false;
            Action renameShip = delegate { RenameShip(); };
            okButton.GetComponent<UiButton>().add_OnTriggered(renameShip);
        }
    }

    internal static void CloseRenameShipDialogue()
    {
        renameShipWindow.gameObject.SetActive(false);
        clickBlocker.gameObject.SetActive(false);
        if (lockHandle != null) lockHandle.Stop();
    }
}