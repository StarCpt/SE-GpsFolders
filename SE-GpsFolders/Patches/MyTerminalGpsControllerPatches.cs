using GpsFolders.Rows;
using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI.HudViewers;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Screens.Terminal;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Game;
using VRageMath;

#pragma warning disable IDE0051
namespace GpsFolders.Patches;

[HarmonyPatch]
public static class MyTerminalGpsControllerPatches
{
    static string? _currentFolder = null;
    static bool _expandFolders = false;

    static MyGuiControlCheckbox _expandFoldersCheckbox;
    static MyGuiControlTextbox _gpsFolderNameTextBox;
    static CustomIndeterminateCheckbox _checkboxFolderShowOnHud;
    static CustomIndeterminateCheckbox _checkboxFolderAlwaysVisible;
    public static GpsFolderListView gpsListView;

    static Vector3D DistanceMeasuringPosition
    {
        get
        {
            MatrixD? controlledEntityMatrix = MyHudMarkerRender.ControlledEntityMatrix;
            if (!controlledEntityMatrix.HasValue || (!MySession.Static.CameraOnCharacter && MySession.Static.IsCameraUserControlledSpectator()))
            {
                return MyHudMarkerRender.CameraMatrix.Translation;
            }

            MatrixD? localCharacterMatrix = MyHudMarkerRender.LocalCharacterMatrix;
            if (MySession.Static.CameraOnCharacter && localCharacterMatrix.HasValue)
            {
                return localCharacterMatrix.Value.Translation;
            }
            else
            {
                return controlledEntityMatrix.Value.Translation;
            }
        }
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.Init))]
    [HarmonyPostfix]
    public static void Init_Postfix(MyTerminalGpsController __instance, IMyGuiControlsParent controlsParent)
    {
        DeferredFolderChange.Clear();

        _expandFoldersCheckbox = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("ExpandFoldersCheckbox");
        _gpsFolderNameTextBox = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("GpsFolderNameTextBox");
        _checkboxFolderShowOnHud = (CustomIndeterminateCheckbox)controlsParent.Controls.GetControlByName("checkFolderShowOnHud");
        _checkboxFolderAlwaysVisible = (CustomIndeterminateCheckbox)controlsParent.Controls.GetControlByName("checkFolderAlwaysVisible");

        _expandFoldersCheckbox.IsChecked = _expandFolders;
        _expandFoldersCheckbox.IsCheckedChanged += delegate
        {
            _expandFolders = !_expandFolders;
            __instance.PopulateList();
        };

        _gpsFolderNameTextBox.TextChanged += OnFolderNameTextboxTextChanged;
        _gpsFolderNameTextBox.EnterPressed += textbox => DeferredFolderChange.Apply(__instance);
        _gpsFolderNameTextBox.FocusChanged += (textbox, focused) =>
        {
            if (!focused)
            {
                DeferredFolderChange.Apply(__instance);
            }
        };

        _checkboxFolderShowOnHud.IsCheckedChanged += checkbox =>
        {
            if (checkbox.State != CheckStateEnum.Indeterminate)
            {
                foreach (var item in __instance.m_listboxGps.SelectedItems)
                {
                    if (item is GpsFolderRow folderRow)
                    {
                        Helpers.SetFolderShowOnHud(folderRow.Name, checkbox.State is CheckStateEnum.Checked);
                    }
                    else if (item is UnsortedGpsFolderRow)
                    {
                        Helpers.SetFolderShowOnHud(null, checkbox.State is CheckStateEnum.Checked);
                    }
                }
            }
        };

        _checkboxFolderAlwaysVisible.IsCheckedChanged += checkbox =>
        {
            if (checkbox.State != CheckStateEnum.Indeterminate)
            {
                foreach (var item in __instance.m_listboxGps.SelectedItems)
                {
                    if (item is GpsFolderRow folderRow)
                    {
                        Helpers.SetFolderAlwaysVisible(folderRow.Name, checkbox.State is CheckStateEnum.Checked);
                    }
                    else if (item is UnsortedGpsFolderRow)
                    {
                        Helpers.SetFolderAlwaysVisible(null, checkbox.State is CheckStateEnum.Checked);
                    }
                }
            }
        };

        __instance.m_panelGpsName.EnterPressed += textbox => SaveGpsName();
        __instance.m_panelGpsName.FocusChanged += (textbox, focused) =>
        {
            if (!focused && !__instance.m_listboxGps.HasFocus)
            {
                SaveGpsName();
            }
        };

        __instance.m_listboxGps.ItemMouseOver += listbox =>
        {
            if (listbox.MouseOverItem is { } item && item is not NonGpsRow && MySector.MainCamera != null)
            {
                var gps = (MyGps)item.UserData;
                var gpsPos = gps.Coords;
                var myPos = DistanceMeasuringPosition;
                double dist = Vector3D.Distance(myPos, gpsPos);

                // reuse strb from tooltip
                item.ToolTip ??= new();
                var strb = item.ToolTip.ToolTips.FirstOrDefault()?.Text ?? new();
                strb.Clear();
                strb.AppendLine(gps.Name);
                MyHudMarkerRender.AppendDistance(strb, dist);

                item.ToolTip.ToolTips.Clear();
                item.ToolTip.AddToolTip(strb.ToString());
            }
        };

        void SaveGpsName()
        {
            if (__instance.m_listboxGps.SelectedItems.Count == 1 && __instance.m_listboxGps.SelectedItems[0] is not NonGpsRow)
            {
                __instance.TrySync();
            }
        }
    }

    static class DeferredFolderChange
    {
        public static string? NewFolder;
        public static readonly List<MyGps> Gpses = [];

        public static void Clear()
        {
            NewFolder = null;
            Gpses.Clear();
        }

        public static void Apply(MyTerminalGpsController controller)
        {
            if (string.IsNullOrWhiteSpace(NewFolder))
            {
                NewFolder = null;
            }

            if (Gpses.Count != 0 && (NewFolder is null || Extensions.IsFolderIdValid(NewFolder)))
            {
                bool folderChanged = false;

                foreach (var gps in Gpses)
                {
                    if (gps.GetFolderId() != NewFolder)
                    {
                        gps.SetFolderId(NewFolder);
                        folderChanged = true;
                    }
                }

                if (folderChanged)
                {
                    controller.PopulateList();
                }
            }
        }
    }

    private static void OnFolderNameTextboxTextChanged(MyGuiControlTextbox textbox)
    {
        if (MyGuiScreenTerminal.m_instance?.m_controllerGps?.m_listboxGps is { } listbox)
        {
            DeferredFolderChange.Clear();
            DeferredFolderChange.NewFolder = textbox.Text;
            DeferredFolderChange.Gpses.AddRange(listbox.SelectedItems.Where(i => i is not NonGpsRow).Select(i => (MyGps)i.UserData));
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

        ___m_listboxGps.ScrollBar.Init(___m_listboxGps.Items.Count, ___m_listboxGps.VisibleRowsCount);
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

                if (_gpsFolderNameTextBox != null)
                {
                    _gpsFolderNameTextBox.Enabled = true;
                    SetFolderNameTextboxTextNoEvent(allGpsesAreInSameFolder ? (folderId ?? "") : "");
                }

                __instance.m_buttonCopy.Enabled = true;
            }
            return true;
        }
    }

    static void SetFolderNameTextboxTextNoEvent(string text)
    {
        if (_gpsFolderNameTextBox != null)
        {
            _gpsFolderNameTextBox.TextChanged -= OnFolderNameTextboxTextChanged;
            _gpsFolderNameTextBox.Text = text;
            _gpsFolderNameTextBox.TextChanged += OnFolderNameTextboxTextChanged;
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
            EnableAndShowCheckboxes(true, false, false, false);

            if (folderCount != 0 && gpsCount != 0)
            {
                __instance.ClearRight();

                // disable all edit boxes
                __instance.EnableEditBoxes(false, false, false);
                __instance.m_buttonCopy.Enabled = false;

                _gpsFolderNameTextBox?.Enabled = false;
                SetFolderNameTextboxTextNoEvent("");
            }
            else if (folderCount > 0)
            {
                __instance.EnableEditBoxes(false, false, true);
                __instance.m_buttonCopy.Enabled = true;

                if (senderListbox.SelectedItems.All(i => i is GpsFolderRow))
                {
                    EnableAndShowCheckboxes(false, false, true, true);
                }

                if (folderCount == 1 && senderListbox.SelectedItems[0] is GpsFolderRow folderRow)
                {
                    __instance.ClearRightExceptName(folderRow.Name);
                    __instance.m_panelGpsName.Text = folderRow.Name;
                    __instance.m_panelGpsName.Enabled = true;

                    if (Helpers.TryGetFolderGpses(folderRow.Name, out var gpses))
                    {
                        _checkboxFolderShowOnHud.SkipIndeterminateState = false;
                        _checkboxFolderAlwaysVisible.SkipIndeterminateState = false;

                        int totalCount = 0;
                        int showOnHudCount = 0;
                        int alwaysVisibleCount = 0;
                        foreach (var gps in gpses)
                        {
                            totalCount++;
                            if (gps.ShowOnHud)
                                showOnHudCount++;
                            if (gps.AlwaysVisible)
                                alwaysVisibleCount++;
                        }

                        _checkboxFolderShowOnHud.State = showOnHudCount == totalCount ? CheckStateEnum.Checked : (showOnHudCount == 0 ? CheckStateEnum.Unchecked : CheckStateEnum.Indeterminate);
                        _checkboxFolderAlwaysVisible.State = alwaysVisibleCount == totalCount ? CheckStateEnum.Checked : (alwaysVisibleCount == 0 ? CheckStateEnum.Unchecked : CheckStateEnum.Indeterminate);

                        _checkboxFolderShowOnHud.SkipIndeterminateState = true;
                        _checkboxFolderAlwaysVisible.SkipIndeterminateState = true;
                    }
                }
                else
                {
                    __instance.ClearRight();
                }

                _gpsFolderNameTextBox?.Enabled = false;
                SetFolderNameTextboxTextNoEvent("");
            }
            return false;
        }
        else // vanilla behavior
        {
            _gpsFolderNameTextBox?.Enabled = true;
            EnableAndShowCheckboxes(true, true, false, false);
            return true;
        }

        void EnableAndShowCheckboxes(bool showGpsCheckboxes, bool enableGpsCheckboxes, bool showFolderCheckboxes, bool enableFolderCheckboxes)
        {
            __instance.m_checkGpsShowOnHud.Visible = showGpsCheckboxes;
            __instance.m_checkGpsAlwaysVisible.Visible = showGpsCheckboxes;
            __instance.m_checkGpsShowOnHud.Enabled = enableGpsCheckboxes;
            __instance.m_checkGpsAlwaysVisible.Enabled = enableGpsCheckboxes;
            _checkboxFolderShowOnHud.Visible = showFolderCheckboxes;
            _checkboxFolderAlwaysVisible.Visible = showFolderCheckboxes;
            _checkboxFolderShowOnHud.Enabled = enableFolderCheckboxes;
            _checkboxFolderAlwaysVisible.Enabled = enableFolderCheckboxes;
        }
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.SetEnabledStates))]
    [HarmonyPostfix]
    public static void SetEnabledStates_Postfix(MyTerminalGpsController __instance, MyGuiControlListbox senderListbox)
    {
        int folderCount = senderListbox.SelectedItems.Count(i => i is NonGpsRow);
        if (senderListbox.SelectedItems.Count != 0 && folderCount == 0)
        {
            __instance.m_buttonCopy.Enabled = true;
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

    [HarmonyPatch]
    static class Patch_OnNameChanged_FindInsertionIndex
    {
        [HarmonyTargetMethod]
        public static MethodInfo TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(MyTerminalGpsController)).Single(i => i.Name.Contains("<OnNameChanged>g__FindInsertionIndex"));
        }

        [HarmonyPrefix]
        public static bool Prefix(IReadOnlyList<MyGuiControlListbox.Item> items, MyGuiControlListbox.Item item, ref int __result)
        {
            string? folderId = ((MyGps)item.UserData).GetFolderId();
            int firstFolderItemIndex = items.Findindex(i => folderId is null ? i is UnsortedGpsFolderRow : (i is GpsFolderRow folderRow && folderRow.Name == folderId));
            if (firstFolderItemIndex is -1)
            {
                __result = items.Count;
                return false;
            }
            firstFolderItemIndex++;

            int lastFolderItemIndex = items.Count - 1;
            for (int i = firstFolderItemIndex + 1; i < items.Count; i++)
            {
                if (items[i] is NonGpsRow)
                {
                    lastFolderItemIndex = i - 1;
                    break;
                }
            }

            int lowerBound = firstFolderItemIndex;
            int upperBound = lastFolderItemIndex + 1;
            while (lowerBound < upperBound)
            {
                int num3 = (lowerBound + upperBound) / 2;
                if (item.Text.CompareToIgnoreCase(items[num3].Text) > 0)
                {
                    lowerBound = num3 + 1;
                }
                else
                {
                    upperBound = num3;
                }
            }

            __result = lowerBound;
            return false;
        }
    }

    [HarmonyPatch(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.OnButtonPressedCopy))]
    [HarmonyPrefix]
    public static bool OnButtonPressedCopy_Prefix(MyGuiControlButton sender, MyGuiControlListbox ___m_listboxGps)
    {
        if (___m_listboxGps.SelectedItems.Count == 0)
            return false;

        IComparer<MyGps> gpsComparer = Comparer<MyGps>.Create(MyTerminalGpsController.SortingComparison);

        List<string> gpsLines = [];
        foreach (var item in ___m_listboxGps.SelectedItems)
        {
            if (item is GpsFolderRow folder)
            {
                if (Helpers.TryGetFolderGpses(folder.Name, out var gpses))
                {
                    foreach (var gps in gpses.OrderBy(i => i, gpsComparer))
                    {
                        gpsLines.Add(gps.ToString() + folder.Name + ':');
                    }
                }
            }
            else if (item is UnsortedGpsFolderRow unsortedFolder)
            {
                var gpses = Helpers.GetUnsortedGpses();
                foreach (var gps in gpses.OrderBy(i => i, gpsComparer))
                {
                    gpsLines.Add(gps.ToString());
                }
            }
            else if (item.TryGetFolderId(out string folderName))
            {
                gpsLines.Add(((MyGps)item.UserData).ToString() + folderName + ':');
            }
            else
            {
                gpsLines.Add(((MyGps)item.UserData).ToString());
            }
        }

        if (gpsLines.Count > 0)
        {
            MyVRage.Platform.System.Clipboard = string.Join("\n", gpsLines);
        }

        return false;
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
        DeferredFolderChange.Clear();
        _expandFoldersCheckbox = null!;
        _gpsFolderNameTextBox = null!;
        _checkboxFolderShowOnHud = null!;
        _checkboxFolderAlwaysVisible = null!;
        gpsListView = null!;
    }
}
