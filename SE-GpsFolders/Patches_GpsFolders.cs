using GpsFolders.Rows;
using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Screens.Terminal;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

#pragma warning disable IDE0051
namespace GpsFolders;

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
            IsChecked = MyTerminalGpsControllerPatches.expandFoldersChecked,
        };
        expandFoldersCheckbox.SetToolTip(new MyToolTips("Expand Folders"));
        gpsPage.Controls.Add(expandFoldersCheckbox);

        MyGuiControlBase textGpsY = gpsPage.GetControlByName("textGpsY");
        MyGuiControlBase buttonFromCurrent = gpsPage.GetControlByName("buttonFromCurrent");

        MyGuiControlButton showFolderOnHudButton = new MyGuiControlButton
        {
            Text = "Show All",
            Position = new Vector2(buttonFromCurrent.Position.X + 0.43f + 0.001f, buttonFromCurrent.Position.Y),
            Name = "ShowFolderOnHudButton",
            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
            VisualStyle = MyGuiControlButtonStyleEnum.Rectangular,
            IsAutoScaleEnabled = true,
            IsAutoEllipsisEnabled = true,
            ShowTooltipWhenDisabled = false,
        };
        showFolderOnHudButton.Size = new Vector2(textGpsY.Size.X / 2.06f, 48f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y);
        showFolderOnHudButton.SetToolTip("Show all entries in the folder on HUD");
        gpsPage.Controls.Add(showFolderOnHudButton);

        MyGuiControlButton hideFolderOnHudButton = new MyGuiControlButton
        {
            Text = "Hide All",
            Position = new Vector2(showFolderOnHudButton.Position.X + textGpsY.Size.X + 0.0002f, showFolderOnHudButton.Position.Y),
            Name = "HideFolderOnHudButton",
            OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
            VisualStyle = MyGuiControlButtonStyleEnum.Rectangular,
            IsAutoScaleEnabled = true,
            IsAutoEllipsisEnabled = true,
            ShowTooltipWhenDisabled = false,
        };
        hideFolderOnHudButton.Size = showFolderOnHudButton.Size;
        hideFolderOnHudButton.SetToolTip("Hide all entries in the folder from HUD");
        gpsPage.Controls.Add(hideFolderOnHudButton);

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
    }
}

[HarmonyPatch]
public static class MyTerminalGpsControllerPatches
{
    public const string MISC_GPS_SEPARATOR_NAME = "--------------------------------------------------";

    public static string currentFolderName = null;
    public static bool expandFoldersChecked = false;

    static MyGuiControlCheckbox m_expandFoldersCheckbox;
    static MyGuiControlButton m_showFolderOnHudButton;
    static MyGuiControlButton m_hideFolderOnHudButton;
    static MyGuiControlTextbox m_gpsFolderNameTextBox;

    public static GpsFolderListView gpsListView;

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.Init))]
    [HarmonyPostfix]
    public static void Init_Postfix(MyTerminalGpsController __instance, IMyGuiControlsParent controlsParent, MyGuiControlSearchBox ___m_searchBox, MyGuiControlListbox ___m_listboxGps)
    {
        (m_expandFoldersCheckbox = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("ExpandFoldersCheckbox")).IsCheckedChanged += delegate
        {
            expandFoldersChecked = !expandFoldersChecked;
            __instance.PopulateList();
        };

        (m_showFolderOnHudButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("ShowFolderOnHudButton")).ButtonClicked += delegate
        {
            SetSelectedFoldersShowOnHud(true);
        };

        (m_hideFolderOnHudButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("HideFolderOnHudButton")).ButtonClicked += delegate
        {
            SetSelectedFoldersShowOnHud(false);
        };

        m_gpsFolderNameTextBox = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("GpsFolderNameTextBox");
        m_gpsFolderNameTextBox.EnterPressed += textbox =>
        {
            ___m_listboxGps.SelectedItems.Where(i => i != null && i is not NonGpsRow && i.UserData is MyGps).ForEach(i => ((MyGps)i.UserData).SetFolderId(textbox.Text));
            __instance.PopulateList();
        };
        m_gpsFolderNameTextBox.Enabled = false;

        void SetSelectedFoldersShowOnHud(bool showOnHud)
        {
            foreach (MyGuiControlListbox.Item item in ___m_listboxGps.SelectedItems)
            {
                if (item is GpsFolderRow folder)
                {
                    Helpers.SetFolderShowOnHud(folder.Name, showOnHud);
                }
                else if (item is UnsortedGpsFolderRow separator)
                {
                    Helpers.SetUnsortedFolderShowOnHud(showOnHud);
                }
            }
        }
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.PopulateList))]
    [HarmonyPrefix]
    public static bool PopulateList_Prefix(MyTerminalGpsController __instance, string ___m_searchString, MyGuiControlListbox ___m_listboxGps, List<MyGps> ___m_gpsListCache)
    {
        //object selectedRow = ___m_tableIns.SelectedRow?.UserData;
        //int? selectedIndex = ___m_tableIns.SelectedRowIndex;

        ___m_gpsListCache.Clear();
        MyGps[] selectedGpses = ___m_listboxGps.SelectedItems.Select(i => (MyGps)i.UserData).ToArray();

        MyGps lastSelectedGps = selectedGpses.LastOrDefault();
        int lastSelectedItemIndex = selectedGpses.Length > 0 ? ___m_listboxGps.Items.IndexOf(___m_listboxGps.GetLastSelected()) : -1;

        __instance.ClearList();

        if (MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId))
        {
            gpsListView = GpsFolderListView.Create(MySession.Static.Gpss[MySession.Static.LocalPlayerId].Select(i => i.Value));

            var itemsToAdd = gpsListView.GetView(ref currentFolderName, ___m_searchString, expandFoldersChecked);
            ___m_gpsListCache.AddRange(itemsToAdd.Select(i => (MyGps)i.UserData));

            //___m_gpsListCache.Sort(SortingComparison);
            //AddRangeToList(___m_listboxGps, ___m_gpsListCache);
            itemsToAdd.ForEach(i => ___m_listboxGps.Add(i));
        }

        __instance.EnableEditBoxes(enable: false);
        if (selectedGpses.Length > 0)
        {
            foreach (MyGuiControlListbox.Item item in ___m_listboxGps.Items)
            {
                if (selectedGpses.Contains((MyGps)item.UserData))
                {
                    ___m_listboxGps.SelectedItems.Add(item);
                    __instance.SetEnabledStates(___m_listboxGps);
                    __instance.SetDeleteButtonEnabled(enabled: true);
                }
            }

            if (___m_listboxGps.SelectedItems.Count == 0 && selectedGpses.Length != 0)
            {
                if (lastSelectedItemIndex >= ___m_listboxGps.Items.Count)
                {
                    lastSelectedItemIndex = ___m_listboxGps.Items.Count - 1;
                }

                if (lastSelectedItemIndex != -1)
                {
                    ___m_listboxGps.SelectSingleItem(___m_listboxGps.Items[lastSelectedItemIndex]);
                    if (___m_listboxGps.SelectedItems.Count != 0)
                    {
                        __instance.SetDeleteButtonEnabled(enabled: true);
                        __instance.FillRight((MyGps)___m_listboxGps.SelectedItems[0].UserData);
                    }
                }
            }
        }

        ___m_listboxGps.ScrollToFirstSelection();
        if (selectedGpses.Length > 0)
        {
            __instance.FillRight();
        }

        return false;
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.PopulateList))]
    [HarmonyPostfix]
    public static void PopulateList_Postfix(MyTerminalGpsController __instance, string ___m_searchString, MyGuiControlListbox ___m_listboxGps)
    {
        m_showFolderOnHudButton.SetEnabled(string.IsNullOrWhiteSpace(___m_searchString));
        m_hideFolderOnHudButton.SetEnabled(string.IsNullOrWhiteSpace(___m_searchString));
    }

    private static void AddRangeToList(MyGuiControlListbox m_listboxGps, IEnumerable<MyGps> gpses)
    {
        foreach (var gps in gpses)
        {
            var strb = new StringBuilder(gps.Name);
            MyGuiControlListbox.Item item = new MyGuiControlListbox.Item(ref strb, strb.ToString(), null, gps);
            item.ColorMask = MyTerminalGpsController.GetGpsColor(gps);
            m_listboxGps.Add(item);
        }
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.OnListboxItemsSelected))]
    [HarmonyPostfix]
    public static void OnListboxItemsSelected_Postfix(
        MyGuiControlListbox senderListbox,
        MyGuiControlTextbox ___m_panelGpsName,
        MyGuiControlMultilineEditableText ___m_panelGpsDesc,
        MyGuiControlTextbox ___m_xCoord,
        MyGuiControlTextbox ___m_yCoord,
        MyGuiControlTextbox ___m_zCoord,
        MyGuiControlSlider ___m_sliderHue,
        MyGuiControlSlider ___m_sliderSaturation,
        MyGuiControlSlider ___m_sliderValue,
        MyGuiControlTextbox ___m_textBoxHex,
        MyGuiControlCheckbox ___m_checkGpsShowOnHud,
        MyGuiControlCheckbox ___m_checkGpsAlwaysVisible,
        MyGuiControlButton ___m_buttonCopy,
        MyGuiControlSearchBox ___m_searchBox)
    {
        var selectedItem = senderListbox.GetLastSelected();
        if (selectedItem is NonGpsRow)
        {
            ___m_panelGpsDesc.Enabled = false;
            ___m_xCoord.Enabled = false;
            ___m_yCoord.Enabled = false;
            ___m_zCoord.Enabled = false;
            ___m_sliderHue.Enabled = false;
            ___m_sliderSaturation.Enabled = false;
            ___m_sliderValue.Enabled = false;
            ___m_textBoxHex.Enabled = false;
            ___m_checkGpsShowOnHud.Enabled = false;
            ___m_checkGpsAlwaysVisible.Enabled = false;

            if (m_gpsFolderNameTextBox != null)
            {
                m_gpsFolderNameTextBox.Enabled = false;
                m_gpsFolderNameTextBox.Text = "";
            }

            if (selectedItem is GpsFolderRow)
            {
                ___m_panelGpsName.Enabled = string.IsNullOrWhiteSpace(___m_searchBox.SearchText);
                ___m_buttonCopy.Enabled = true;

                m_showFolderOnHudButton?.SetEnabled(true);
                m_hideFolderOnHudButton?.SetEnabled(true);
            }
            else if (selectedItem is UnsortedGpsFolderRow)
            {
                ___m_panelGpsName.Enabled = false;
                ___m_buttonCopy.Enabled = true;

                m_showFolderOnHudButton?.SetEnabled(true);
                m_hideFolderOnHudButton?.SetEnabled(true);
            }
        }
        else
        {
            ___m_checkGpsShowOnHud.Enabled = true;
            ___m_checkGpsAlwaysVisible.Enabled = true;

            m_showFolderOnHudButton?.SetEnabled(false);
            m_hideFolderOnHudButton?.SetEnabled(false);
            if (m_gpsFolderNameTextBox != null)
            {
                if (selectedItem != null && selectedItem.TryGetFolderId(out string tag))
                    m_gpsFolderNameTextBox.Text = tag;
                else
                    m_gpsFolderNameTextBox.Text = "";
            }
        }
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.EnableEditBoxes))]
    [HarmonyPostfix]
    public static void EnableEditBoxes_Postfix(bool enable)
    {
        if (m_gpsFolderNameTextBox != null)
            m_gpsFolderNameTextBox.Enabled = enable;
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.OnListboxDoubleClick))]
    [HarmonyPrefix]
    public static bool OnListboxDoubleClick_Prefix(MyGuiControlListbox senderListbox)
    {
        bool runOriginal = !(senderListbox.SelectedItems.FirstOrDefault() is NonGpsRow);
        if (senderListbox.SelectedItems.FirstOrDefault() is GpsFolderRow folder)
        {
            if (currentFolderName == null)
                currentFolderName = folder.Name;
            else
                currentFolderName = null;

            MyGuiScreenTerminal.m_instance?.m_controllerGps?.PopulateList();
            return false;
        }
        return runOriginal;
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.ToggleShowOnHud))]
    [HarmonyPrefix]
    public static bool ToggleShowOnHud_Prefix(MyGuiControlListbox senderListbox)
    {
        bool runOriginal = !(senderListbox.SelectedItems.FirstOrDefault() is NonGpsRow);
        return runOriginal;
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.OnNameChanged))]
    [HarmonyPrefix]
    public static bool OnNameChanged_Prefix(MyGuiControlTextbox senderTextbox, MyGuiControlListbox ___m_listboxGps) // called when the gps name field changes
        {
        if (___m_listboxGps.SelectedItems.FirstOrDefault() == null)
        {
            return false;
        }

        bool runOriginal = !(___m_listboxGps.SelectedItems.FirstOrDefault() is NonGpsRow);
        MyGuiControlListbox.Item selectedRow = ___m_listboxGps.SelectedItems.FirstOrDefault();

        if (selectedRow is GpsFolderRow folder)
        {
            string oldFolderName = folder.Name;
            string newFolderName = senderTextbox.Text;
            if (gpsListView.TrySetFolderId(folder.Name, newFolderName))
            {
                if (oldFolderName == currentFolderName)
                {
                    currentFolderName = newFolderName;
                }

                ___m_listboxGps.Clear();
                gpsListView.GetView(ref currentFolderName, gpsListView.LastSearchText, expandFoldersChecked).ForEach(row => ___m_listboxGps.Add(row));

                ___m_listboxGps.SelectSingleItem(___m_listboxGps.Items.FirstOrDefault(i => i is GpsFolderRow row && row.Name == newFolderName));
                ___m_listboxGps.ScrollToFirstSelection();
            }
        }

        return runOriginal;
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.OnButtonPressedCopy))]
    [HarmonyPrefix]
    public static bool OnButtonPressedCopy_Prefix(MyGuiControlButton sender, MyGuiControlListbox ___m_listboxGps)
    {
        if (___m_listboxGps.SelectedItems.FirstOrDefault() == null)
            return false;

        bool runOriginal = !(___m_listboxGps.SelectedItems.FirstOrDefault() is NonGpsRow);
        if (___m_listboxGps.SelectedItems.FirstOrDefault() is GpsFolderRow folder)
        {
            Helpers.CopyFolderToClipboard(folder.Name);
            return false;
        }
        if (___m_listboxGps.SelectedItems.FirstOrDefault() is UnsortedGpsFolderRow unsorted)
        {
            Helpers.CopyUnsortedGpsesToClipboard();
            return false;
        }
        else if (___m_listboxGps.SelectedItems.FirstOrDefault().TryGetFolderId(out string tag))
        {
            MyVRage.Platform.System.Clipboard = ___m_listboxGps.SelectedItems.FirstOrDefault().UserData.ToString() + tag + ':';
            return false;
        }

        return runOriginal;
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.Delete))]
    [HarmonyPrefix]
    public static bool Delete_Prefix(MyGuiControlListbox ___m_listboxGps)
    {
        bool runOriginal = !(___m_listboxGps.SelectedItems.FirstOrDefault() is NonGpsRow);
        if (___m_listboxGps.SelectedItems.FirstOrDefault() is GpsFolderRow folder)
        {
            if (currentFolderName == folder.Name)
            {
                currentFolderName = null;
            }

            gpsListView?.DeleteFolder(folder.Name);
            return false;
        }
        return runOriginal;
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.Close))]
    [HarmonyPostfix]
    public static void Close_Postfix()
    {
        m_expandFoldersCheckbox = null;
        m_showFolderOnHudButton = null;
        m_hideFolderOnHudButton = null;
        m_gpsFolderNameTextBox = null;
        gpsListView = null;
        //MiscellaneousPatches.m_showDistanceColumnCheckbox = null;
    }
}
