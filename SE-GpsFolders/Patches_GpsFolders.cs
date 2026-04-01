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
    public static void Init_Postfix(MyTerminalGpsController __instance, IMyGuiControlsParent controlsParent)
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
        m_gpsFolderNameTextBox.EnterPressed += textbox => ApplyFolderName(textbox.Text);
        m_gpsFolderNameTextBox.FocusChanged += (textbox, focused) =>
        {
            if (!focused)
            {
                ApplyFolderName(((MyGuiControlTextbox)textbox).Text);
            }
        };
        m_gpsFolderNameTextBox.Enabled = false;

        __instance.m_panelGpsName.EnterPressed += textbox => SaveGpsName();
        __instance.m_panelGpsName.FocusChanged += (textbox, focused) =>
        {
            if (!focused)
            {
                SaveGpsName();
            }
        };

        void SetSelectedFoldersShowOnHud(bool showOnHud)
        {
            foreach (MyGuiControlListbox.Item item in __instance.m_listboxGps.SelectedItems)
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

        void ApplyFolderName(string? newFolder)
        {
            if (string.IsNullOrWhiteSpace(newFolder))
            {
                newFolder = null;
            }

            bool folderChanged = false;

            foreach (var gps in __instance.m_listboxGps.SelectedItems.Where(i => i != null && i is not NonGpsRow && i.UserData is MyGps).Select(i => (MyGps)i.UserData))
            {
                if (gps.GetFolderId() != newFolder)
                {
                    gps.SetFolderId(newFolder);
                    folderChanged = true;
                }
            }

            if (folderChanged)
            {
                __instance.PopulateList();
            }
        }

        void SaveGpsName()
        {
            if (__instance.m_listboxGps.SelectedItems.Count == 1 && __instance.m_listboxGps.SelectedItems[0] is not NonGpsRow)
            {
                __instance.TrySync();
            }
        }
    }

    private static void M_panelGpsName_FocusChanged(MyGuiControlBase arg1, bool arg2)
    {
        throw new NotImplementedException();
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.PopulateList))]
    [HarmonyPrefix]
    public static bool PopulateList_Prefix(MyTerminalGpsController __instance, string ___m_searchString, MyGuiControlListbox ___m_listboxGps, List<MyGps> ___m_gpsListCache)
    {
        ___m_gpsListCache.Clear();
        var selectedItems = ___m_listboxGps.SelectedItems.ToList();

        __instance.ClearList();

        if (MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId))
        {
            gpsListView = GpsFolderListView.Create(MySession.Static.Gpss[MySession.Static.LocalPlayerId].Select(i => i.Value));

            var itemsToAdd = gpsListView.GetView(ref currentFolderName, ___m_searchString, expandFoldersChecked);
            ___m_gpsListCache.AddRange(itemsToAdd.Select(i => (MyGps)i.UserData));

            itemsToAdd.ForEach(i => ___m_listboxGps.Add(i));
        }

        __instance.EnableEditBoxes(false);

        if (selectedItems.Count > 0)
        {
            foreach (var row in selectedItems)
            {
                int index = -1;
                if (row is UnsortedGpsFolderRow)
                {
                    // nothing
                    index = ___m_listboxGps.Items.FindIndex(i => i is UnsortedGpsFolderRow);
                }
                else if (row is GpsFolderRow folderRow)
                {
                    index = ___m_listboxGps.Items.FindIndex(i => i is GpsFolderRow row && row.Name == folderRow.Name);
                }
                else // is normal gps
                {
                    index = ___m_listboxGps.Items.FindIndex(i => i is not NonGpsRow && (MyGps)i.UserData == (MyGps)row.UserData);
                }

                if (index != -1)
                {
                    ___m_listboxGps.SelectedItems.Add(___m_listboxGps.Items[index]);
                }
            }
        }

        ___m_listboxGps.ScrollToFirstSelection();
        if (___m_listboxGps.SelectedItems.Count != 0)
        {
            ___m_listboxGps.ScrollToFirstSelection();
            __instance.FillRight();
        }
        else
        {
            ___m_listboxGps.ScrollToolbarToTop();
            __instance.ClearRight();
        }

        return false;
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.PopulateList))]
    [HarmonyPostfix]
    public static void PopulateList_Postfix(MyTerminalGpsController __instance, string ___m_searchString, MyGuiControlListbox ___m_listboxGps)
    {
        m_showFolderOnHudButton?.Enabled = string.IsNullOrWhiteSpace(___m_searchString);
        m_hideFolderOnHudButton?.Enabled = string.IsNullOrWhiteSpace(___m_searchString);
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.OnListboxItemsSelected))]
    [HarmonyPrefix]
    public static bool OnListboxItemsSelected_Prefix(MyTerminalGpsController __instance, MyGuiControlListbox senderListbox)
    {
        __instance.TrySync();
        if (senderListbox.SelectedItems.Count > 0 && senderListbox.SelectedItems.Count(i => i is NonGpsRow) > 0)
        {
            __instance.SetDeleteButtonEnabled(true);
            __instance.SetEnabledStates(senderListbox);

            foreach (MyGuiControlListbox.Item item in senderListbox.SelectedItems)
            {
                if (item is not NonGpsRow)
                {
                    item.ColorMask = MyTerminalGpsController.GetGpsColor((MyGps)item.UserData);
                }
            }

            int folderCount = senderListbox.SelectedItems.Count(i => i is NonGpsRow);
            int gpsCount = senderListbox.SelectedItems.Count - folderCount;

            // disable edit boxes if gps and folders are mixed, or if more than 1 folder is selected
            // disable all edit boxes except name if 1 folder is selected
            // vanilla behavior if no folders are selected

            if (folderCount != 0 && gpsCount != 0)
            {
                __instance.ClearRight();

                // disable all edit boxes
                __instance.EnableEditBoxes(false, false, false);
                __instance.m_buttonCopy.Enabled = false;

                m_gpsFolderNameTextBox?.Enabled = false;
                m_gpsFolderNameTextBox?.Text = "";

                m_showFolderOnHudButton?.Enabled = false;
                m_hideFolderOnHudButton?.Enabled = false;
            }
            else if (folderCount > 0)
            {
                __instance.EnableEditBoxes(false, false, true);
                __instance.m_buttonCopy.Enabled = true;

                if (folderCount == 1 && senderListbox.SelectedItems[0] is GpsFolderRow folderRow)
                {
                    __instance.ClearRightExceptName(folderRow.Name);
                    __instance.m_panelGpsName.Text = folderRow.Name;
                    __instance.m_panelGpsName.Enabled = true;
                }
                else
                {
                    __instance.ClearRight();
                }

                m_gpsFolderNameTextBox?.Enabled = false;
                m_gpsFolderNameTextBox?.Text = "";

                m_showFolderOnHudButton?.Enabled = true;
                m_hideFolderOnHudButton?.Enabled = true;
            }
            else // vanilla behavior
            {
                m_gpsFolderNameTextBox?.Enabled = true;
                m_showFolderOnHudButton?.Enabled = false;
                m_hideFolderOnHudButton?.Enabled = false;
                return true;
            }

            return false;
        }
        else
        {
            if (senderListbox.SelectedItems.Count > 0)
            {
                string? folderId = ((MyGps)senderListbox.SelectedItems[0].UserData).GetFolderId();
                bool allGpsesAreInSameFolder = true;
                foreach (var item in senderListbox.SelectedItems)
                {
                    if (folderId != ((MyGps)item.UserData).GetFolderId())
                    {
                        allGpsesAreInSameFolder = false;
                        break;
                    }
                }

                folderId ??= string.Empty;

                if (m_gpsFolderNameTextBox != null)
                {
                    m_gpsFolderNameTextBox.Enabled = true;
                    m_gpsFolderNameTextBox.Text = allGpsesAreInSameFolder ? folderId : "";
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.SetEnabledStates))]
    [HarmonyPrefix]
    public static bool SetEnabledStates_Prefix(MyTerminalGpsController __instance, MyGuiControlListbox senderListbox)
    {
        int folderCount = senderListbox.SelectedItems.Count(i => i is NonGpsRow);
        int gpsCount = senderListbox.SelectedItems.Count - folderCount;

        if (folderCount != 0)
        {
            return false;
        }
        else // vanilla behavior
        {
            m_gpsFolderNameTextBox?.Enabled = true;
            m_showFolderOnHudButton?.Enabled = false;
            m_hideFolderOnHudButton?.Enabled = false;
            return true;
        }
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.EnableEditBoxes))]
    [HarmonyPostfix]
    public static void EnableEditBoxes_Postfix(bool enable)
    {
        m_gpsFolderNameTextBox?.Enabled = enable;
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
    public static bool OnNameChanged_Prefix(MyTerminalGpsController __instance, MyGuiControlTextbox senderTextbox) // called when the gps name field changes
    {
        if (__instance.m_listboxGps.SelectedItems.FirstOrDefault() == null)
        {
            return false;
        }

        bool runOriginal = !(__instance.m_listboxGps.SelectedItems.FirstOrDefault() is NonGpsRow);
        MyGuiControlListbox.Item selectedRow = __instance.m_listboxGps.SelectedItems.FirstOrDefault();

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

                __instance.ClearList();
                gpsListView.GetView(ref currentFolderName, gpsListView.LastSearchText, expandFoldersChecked).ForEach(row => __instance.m_listboxGps.Add(row));

                __instance.m_listboxGps.SelectSingleItem(__instance.m_listboxGps.Items.FirstOrDefault(i => i is GpsFolderRow row && row.Name == newFolderName));
                __instance.m_listboxGps.ScrollToFirstSelection();
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
