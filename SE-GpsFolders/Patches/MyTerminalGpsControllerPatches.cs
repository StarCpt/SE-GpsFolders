using GpsFolders.Rows;
using HarmonyLib;
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

#pragma warning disable IDE0051
namespace GpsFolders.Patches;

[HarmonyPatch]
public static class MyTerminalGpsControllerPatches
{
    static string? _currentFolder = null;
    static bool _expandFolders = false;

    static MyGuiControlCheckbox _expandFoldersCheckbox;
    static MyGuiControlButton _showFolderOnHudButton;
    static MyGuiControlButton _hideFolderOnHudButton;
    static MyGuiControlTextbox _gpsFolderNameTextBox;

    public static GpsFolderListView gpsListView;

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.Init))]
    [HarmonyPostfix]
    public static void Init_Postfix(MyTerminalGpsController __instance, IMyGuiControlsParent controlsParent)
    {
        _expandFoldersCheckbox = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("ExpandFoldersCheckbox");
        _showFolderOnHudButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("ShowFolderOnHudButton");
        _hideFolderOnHudButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("HideFolderOnHudButton");
        _gpsFolderNameTextBox = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("GpsFolderNameTextBox");

        _expandFoldersCheckbox.IsChecked = _expandFolders;
        _expandFoldersCheckbox.IsCheckedChanged += delegate
        {
            _expandFolders = !_expandFolders;
            __instance.PopulateList();
        };

        _showFolderOnHudButton.ButtonClicked += _ => SetSelectedFoldersShowOnHud(true);
        _hideFolderOnHudButton.ButtonClicked += _ => SetSelectedFoldersShowOnHud(false);

        _gpsFolderNameTextBox.EnterPressed += textbox => ApplyFolderName(textbox.Text);
        _gpsFolderNameTextBox.FocusChanged += (textbox, focused) =>
        {
            if (!focused)
            {
                ApplyFolderName(((MyGuiControlTextbox)textbox).Text);
            }
        };

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

            var itemsToAdd = gpsListView.GetView(ref _currentFolder, ___m_searchString, _expandFolders);
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
        _showFolderOnHudButton?.Enabled = string.IsNullOrWhiteSpace(___m_searchString);
        _hideFolderOnHudButton?.Enabled = string.IsNullOrWhiteSpace(___m_searchString);
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

                _gpsFolderNameTextBox?.Enabled = false;
                _gpsFolderNameTextBox?.Text = "";

                _showFolderOnHudButton?.Enabled = false;
                _hideFolderOnHudButton?.Enabled = false;
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

                _gpsFolderNameTextBox?.Enabled = false;
                _gpsFolderNameTextBox?.Text = "";

                _showFolderOnHudButton?.Enabled = true;
                _hideFolderOnHudButton?.Enabled = true;
            }
            else // vanilla behavior
            {
                _gpsFolderNameTextBox?.Enabled = true;
                _showFolderOnHudButton?.Enabled = false;
                _hideFolderOnHudButton?.Enabled = false;
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

                if (_gpsFolderNameTextBox != null)
                {
                    _gpsFolderNameTextBox.Enabled = true;
                    _gpsFolderNameTextBox.Text = allGpsesAreInSameFolder ? folderId : "";
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
            _gpsFolderNameTextBox?.Enabled = true;
            _showFolderOnHudButton?.Enabled = false;
            _hideFolderOnHudButton?.Enabled = false;
            return true;
        }
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.EnableEditBoxes))]
    [HarmonyPostfix]
    public static void EnableEditBoxes_Postfix(bool enable)
    {
        _gpsFolderNameTextBox?.Enabled = enable;
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.OnListboxDoubleClick))]
    [HarmonyPrefix]
    public static bool OnListboxDoubleClick_Prefix(MyGuiControlListbox senderListbox)
    {
        bool runOriginal = !(senderListbox.SelectedItems.FirstOrDefault() is NonGpsRow);
        if (senderListbox.SelectedItems.FirstOrDefault() is GpsFolderRow folder)
        {
            if (_currentFolder == null)
                _currentFolder = folder.Name;
            else
                _currentFolder = null;

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

            if (string.IsNullOrWhiteSpace(newFolderName))
            {
                newFolderName = "";
            }

            if (gpsListView.TrySetFolderId(folder.Name, newFolderName))
            {
                if (oldFolderName == _currentFolder)
                {
                    _currentFolder = newFolderName;
                }

                __instance.ClearList();
                gpsListView.GetView(ref _currentFolder, gpsListView.LastSearchText, _expandFolders).ForEach(row => __instance.m_listboxGps.Add(row));

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
            if (_currentFolder == folder.Name)
            {
                _currentFolder = null;
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
        _expandFoldersCheckbox = null;
        _showFolderOnHudButton = null;
        _hideFolderOnHudButton = null;
        _gpsFolderNameTextBox = null;
        gpsListView = null;
    }
}
