using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace GpsFolders.Patches;

[HarmonyPatch]
public static class MyGuiScreenTerminalPatches
{
    [HarmonyPatch(typeof(MyGuiScreenTerminal), nameof(MyGuiScreenTerminal.CreateGpsPageControls))]
    [HarmonyPostfix]
    public static void CreateGpsPageControls_Postfix(MyGuiControlTabPage gpsPage)
    {
        MyGuiControlLabel expandFoldersLabel = new MyGuiControlLabel()
        {
            Position = new Vector2(-0.196f, -0.267f + 0.011f),
            Name = "ExpandFoldersLabel",
            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
            Text = MyTexts.GetString("Expand Folders"),
            TextScale = 0.7f,
        };
        gpsPage.Controls.Add(expandFoldersLabel);

        MyGuiControlCheckbox expandFoldersCheckbox = new MyGuiControlCheckbox
        {
            Position = new Vector2(-0.191f, -0.277f),
            Name = "ExpandFoldersCheckbox",
            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
        };
        expandFoldersCheckbox.SetToolTip(new MyToolTips("Expand Folders"));
        gpsPage.Controls.Add(expandFoldersCheckbox);

        // put indeterminate checkboxes for folder use in the same spots as the showOnHud/alwaysVisible ones

        MyGuiControlBase checkboxShowOnHud = gpsPage.GetControlByName("checkGpsShowOnHud");
        MyGuiControlBase checkboxAlwaysVisible = gpsPage.GetControlByName("checkGpsAlwaysVisible");
        CustomIndeterminateCheckbox checkboxFolderShowOnHud = new(checkboxShowOnHud.Position, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER)
        {
            Name = "checkFolderShowOnHud",
            Visible = false,
            SkipIndeterminateState = true,
        };
        CustomIndeterminateCheckbox checkboxFolderAlwaysVisible = new(checkboxAlwaysVisible.Position, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER)
        {
            Name = "checkFolderAlwaysVisible",
            Visible = false,
            SkipIndeterminateState = true,
        };
        checkboxFolderShowOnHud.SetTooltip("Display GPS markers in this folder on the HUD.");
        checkboxFolderAlwaysVisible.SetTooltip("Prevents GPS markers in this folder from getting clustered or fading out.");
        gpsPage.Controls.Add(checkboxFolderShowOnHud);
        gpsPage.Controls.Add(checkboxFolderAlwaysVisible);

        MyGuiControlLabel gpsDescriptionLabel = (MyGuiControlLabel)gpsPage.GetControlByName("labelGpsDesc");
        MyGuiControlMultilineEditableText gpsDescriptionText = (MyGuiControlMultilineEditableText)gpsPage.GetControlByName("textGpsDesc");

        MyGuiControlLabel gpsFolderLabel = new MyGuiControlLabel(
            gpsDescriptionLabel.Position,
            new Vector2(0.4f, 0.035f),
            null,
            null,
            0.8f,
            "Blue",
            MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
        {
            Name = "GpsFolderNameLabel",
            Text = "Folder Name:",
        };
        gpsFolderLabel.PositionY -= gpsFolderLabel.Size.Y * 0.25f;
        gpsPage.Controls.Add(gpsFolderLabel);

        MyGuiControlTextbox gpsFolderTextBox = new MyGuiControlTextbox(null, null, 32)
        {
            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
            Position = gpsDescriptionText.Position,
            Size = new Vector2(0.58f, 0.035f),
            Name = "GpsFolderNameTextBox"
        };
        gpsFolderTextBox.PositionY -= gpsFolderLabel.Size.Y * 0.25f;
        gpsFolderTextBox.Enabled = false;
        gpsPage.Controls.Add(gpsFolderTextBox);

        float offset = gpsFolderLabel.Size.Y * 0.5f + gpsFolderTextBox.Size.Y + 0.03f;
        gpsDescriptionLabel.PositionY += offset;
        gpsDescriptionText.PositionY += offset;
        gpsDescriptionText.Size = new Vector2(gpsDescriptionText.Size.X, gpsDescriptionText.Size.Y - offset);

        MyGuiControlBase buttonToClipboard = gpsPage.GetControlByName("buttonToClipboard");

        MyGuiControlButton copyAllGpsesButton = new MyGuiControlButton
        {
            Text = "Copy ALL gpses to clipboard",
            Position = new Vector2(buttonToClipboard.Position.X, buttonToClipboard.Position.Y + buttonToClipboard.Size.Y + 0.005f),
            Name = "CopyAllGpsesButton",
            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM,
            VisualStyle = MyGuiControlButtonStyleEnum.Rectangular,
            IsAutoScaleEnabled = true,
            IsAutoEllipsisEnabled = true,
            ShowTooltipWhenDisabled = false,
        };
        copyAllGpsesButton.Size = new Vector2(buttonToClipboard.Size.X, buttonToClipboard.Size.Y);
        copyAllGpsesButton.SetToolTip("Copy ALL gpses to clipboard");
        copyAllGpsesButton.ButtonClicked += (source) => Helpers.ShowConfirmationDialog("Copy all gpses!", "Are you sure you want to copy ALL of your gpses to clipboard?", CopyAllGpsesToClipboardDialogCallback);
        gpsPage.Controls.Add(copyAllGpsesButton);
    }

    private static void CopyAllGpsesToClipboardDialogCallback(MyGuiScreenMessageBox.ResultEnum result)
    {
        if (result == MyGuiScreenMessageBox.ResultEnum.YES && MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId))
        {
            SortedDictionary<string, List<MyGps>> gpsDict = new SortedDictionary<string, List<MyGps>>();
            StringBuilder strb = new StringBuilder();

            foreach (MyGps item in MySession.Static.Gpss[MySession.Static.LocalPlayerId].Values)
            {
                string tag = item.GetFolderId() ?? string.Empty;

                if (!gpsDict.TryGetValue(tag, out List<MyGps> gpses))
                {
                    gpsDict.Add(tag, gpses = new List<MyGps>());
                }
                gpses.Add(item);
            }

            foreach (var item in gpsDict)
            {
                foreach (var gps in item.Value)
                {
                    strb.Append(gps.ToString());
                    if (item.Key != string.Empty)
                        strb.Append(item.Key).Append(':');
                    strb.AppendLine();
                }
            }

            if (strb.Length > 0)
            {
                strb.Length--;
                MyVRage.Platform.System.Clipboard = strb.ToString();
            }
        }
    }
}
