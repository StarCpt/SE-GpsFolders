using GpsFolders.Rows;
using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace GpsFolders
{
    static class MyGuiScreenTerminalPatches
    {
        [HarmonyPatch(typeof(MyGuiScreenTerminal), "CreateGpsPageControls", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(MyGuiControlTabPage) })]
        static class Patch_CreateGpsPageControls
        {
            static void Postfix(MyGuiControlTabPage gpsPage)
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

                MyGuiControlBase textInsY = gpsPage.GetControlByName("textInsY");
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
                showFolderOnHudButton.Size = new Vector2(textInsY.Size.X / 2.06f, 48f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y);
                showFolderOnHudButton.SetToolTip("Show all entries in the folder on HUD");
                gpsPage.Controls.Add(showFolderOnHudButton);

                MyGuiControlButton hideFolderOnHudButton = new MyGuiControlButton
                {
                    Text = "Hide All",
                    Position = new Vector2(showFolderOnHudButton.Position.X + textInsY.Size.X + 0.0002f, showFolderOnHudButton.Position.Y),
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

                MyGuiControlLabel gpsDescriptionLabel = (MyGuiControlLabel)gpsPage.GetControlByName("labelInsDesc");
                MyGuiControlMultilineEditableText gpsDescriptionText = (MyGuiControlMultilineEditableText)gpsPage.GetControlByName("textInsDesc");

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
    }

    static class MyTerminalGpsControllerPatches
    {
        public const string MISC_GPS_SEPARATOR_NAME = "--------------------------------------------------";

        public static readonly Type _myTerminalGpsControllerType =
            AccessTools.TypeByName("Sandbox.Game.Gui.MyTerminalGpsController");
        public static readonly MethodInfo populateListMethod =
            AccessTools.Method(_myTerminalGpsControllerType, "PopulateList", new Type[] { typeof(string) });
        public static readonly MethodInfo _enableEditBoxesMethod =
            AccessTools.Method(_myTerminalGpsControllerType, "EnableEditBoxes", new Type[] { typeof(bool) });
        public static readonly MethodInfo _setDeleteButtonEnabledMethod =
            AccessTools.Method(_myTerminalGpsControllerType, "SetDeleteButtonEnabled", new Type[] { typeof(bool) });
        public static readonly MethodInfo _fillRightMethod =
            AccessTools.Method(_myTerminalGpsControllerType, "FillRight", new Type[] { typeof(MyGps) });
        public static readonly MethodInfo _fillRightMethod2 =
            AccessTools.Method(_myTerminalGpsControllerType, "FillRight", new Type[] { });

        public static string currentFolderName = null;
        public static bool expandFoldersChecked = false;

        static MyGuiControlCheckbox m_expandFoldersCheckbox;
        static MyGuiControlButton m_showFolderOnHudButton;
        static MyGuiControlButton m_hideFolderOnHudButton;
        static MyGuiControlTextbox m_gpsFolderNameTextBox;

        public static GpsFolderListView gpsListView;

        public static void PopulateList(object instance, string searchString) => populateListMethod.Invoke(instance, new object[] { searchString });

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "Init", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(IMyGuiControlsParent) })]
        static class Patch_Init
        {
            static void Postfix(object __instance, IMyGuiControlsParent controlsParent, MyGuiControlSearchBox ___m_searchBox, MyGuiControlTable ___m_tableIns)
            {
                (m_expandFoldersCheckbox = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("ExpandFoldersCheckbox")).IsCheckedChanged += delegate
                {
                    expandFoldersChecked = !expandFoldersChecked;
                    PopulateList(__instance, ___m_searchBox.SearchText);
                };

                (m_showFolderOnHudButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("ShowFolderOnHudButton")).ButtonClicked += delegate
                {
                    if (___m_tableIns.SelectedRow is GpsFolderRow folder)
                    {
                        Helpers.SetFolderShowOnHud(folder.Name, true);
                    }
                    else if (___m_tableIns.SelectedRow is UnsortedGpsFolderRow separator)
                    {
                        Helpers.SetUnsortedFolderShowOnHud(true);
                    }
                };

                (m_hideFolderOnHudButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("HideFolderOnHudButton")).ButtonClicked += delegate
                {
                    if (___m_tableIns.SelectedRow is GpsFolderRow folder)
                    {
                        Helpers.SetFolderShowOnHud(folder.Name, false);
                    }
                    else if (___m_tableIns.SelectedRow is UnsortedGpsFolderRow separator)
                    {
                        Helpers.SetUnsortedFolderShowOnHud(false);
                    }
                };

                (m_gpsFolderNameTextBox = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("GpsFolderNameTextBox")).TextChanged += textbox =>
                {
                    ___m_tableIns.SelectedRow?.SetFolderId(textbox.Text);
                };
                m_gpsFolderNameTextBox.Enabled = false;
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "PopulateList", MethodType.Normal)]
        [HarmonyPatch(new Type[] {typeof(string) })]
        public static class Patch_PopulateList
        {
            public static bool Prefix(string searchString, object __instance, MyGuiControlTable ___m_tableIns)
            {
                object selectedRow = ___m_tableIns.SelectedRow?.UserData;
                int? selectedIndex = ___m_tableIns.SelectedRowIndex;

                ___m_tableIns.Rows?.Clear();

                if (MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId))
                {
                    gpsListView = GpsFolderListView.Create(MySession.Static.Gpss[MySession.Static.LocalPlayerId].Select(i => i.Value));
                    gpsListView.GetView(ref currentFolderName, searchString, expandFoldersChecked).ForEach(row => ___m_tableIns.Add(row));
                }

                if (selectedRow != null)
                {
                    for (int j = 0; j < ___m_tableIns.RowsCount; j++)
                    {
                        if (selectedRow == ___m_tableIns.GetRow(j).UserData)
                        {
                            ___m_tableIns.SelectedRowIndex = j;
                            EnableEditBoxes(enable: true);
                            SetDeleteButtonEnabled(enabled: true);
                            break;
                        }
                    }

                    if (___m_tableIns.SelectedRow == null)
                    {
                        if (selectedIndex >= ___m_tableIns.RowsCount)
                        {
                            selectedIndex = ___m_tableIns.RowsCount - 1;
                        }

                        ___m_tableIns.SelectedRowIndex = selectedIndex;
                        if (___m_tableIns.SelectedRow != null)
                        {
                            EnableEditBoxes(enable: true);
                            SetDeleteButtonEnabled(enabled: true);
                            FillRight2((MyGps)___m_tableIns.SelectedRow.UserData);
                        }
                    }
                }

                ___m_tableIns.ScrollToSelection();
                if (selectedRow != null)
                {
                    FillRight();
                }

                return false;

                void EnableEditBoxes(bool enable)
                {
                    _enableEditBoxesMethod.Invoke(__instance, new object[] { enable });
                }

                void SetDeleteButtonEnabled(bool enabled)
                {
                    _setDeleteButtonEnabledMethod.Invoke(__instance, new object[] { enabled });
                }

                void FillRight()
                {
                    _fillRightMethod2.Invoke(__instance, null);
                }

                void FillRight2(MyGps ins)
                {
                    _fillRightMethod.Invoke(__instance, new object[] { ins });
                }
            }

            static void Postfix(string searchString, object __instance, MyGuiControlTable ___m_tableIns)
            {
                m_showFolderOnHudButton.SetEnabled(string.IsNullOrWhiteSpace(searchString));
                m_hideFolderOnHudButton.SetEnabled(string.IsNullOrWhiteSpace(searchString));
            }
        }
        
        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "OnTableItemSelected", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(MyGuiControlTable), typeof(MyGuiControlTable.EventArgs) })]
        static class Patch_OnTableItemSelected
        {
            static void Postfix(
                MyGuiControlTable sender,
                MyGuiControlTextbox ___m_panelInsName,
                MyGuiControlMultilineEditableText ___m_panelInsDesc,
                MyGuiControlTextbox ___m_xCoord,
                MyGuiControlTextbox ___m_yCoord,
                MyGuiControlTextbox ___m_zCoord,
                MyGuiControlSlider ___m_sliderHue,
                MyGuiControlSlider ___m_sliderSaturation,
                MyGuiControlSlider ___m_sliderValue,
                MyGuiControlTextbox ___m_textBoxHex,
                MyGuiControlCheckbox ___m_checkInsShowOnHud,
                MyGuiControlCheckbox ___m_checkInsAlwaysVisible,
                MyGuiControlButton ___m_buttonCopy,
                MyGuiControlSearchBox ___m_searchBox)
            {
                if (sender.SelectedRow is NonGpsRow)
                {
                    ___m_panelInsDesc.Enabled = false;
                    ___m_xCoord.Enabled = false;
                    ___m_yCoord.Enabled = false;
                    ___m_zCoord.Enabled = false;
                    ___m_sliderHue.Enabled = false;
                    ___m_sliderSaturation.Enabled = false;
                    ___m_sliderValue.Enabled = false;
                    ___m_textBoxHex.Enabled = false;
                    ___m_checkInsShowOnHud.Enabled = false;
                    ___m_checkInsAlwaysVisible.Enabled = false;

                    if (m_gpsFolderNameTextBox != null)
                    {
                        m_gpsFolderNameTextBox.Enabled = false;
                        m_gpsFolderNameTextBox.Text = "";
                    }

                    if (sender.SelectedRow is GpsFolderRow)
                    {
                        ___m_panelInsName.Enabled = string.IsNullOrWhiteSpace(___m_searchBox.SearchText);
                        ___m_buttonCopy.Enabled = true;

                        m_showFolderOnHudButton?.SetEnabled(true);
                        m_hideFolderOnHudButton?.SetEnabled(true);
                    }
                    else if (sender.SelectedRow is UnsortedGpsFolderRow)
                    {
                        ___m_panelInsName.Enabled = false;
                        ___m_buttonCopy.Enabled = true;

                        m_showFolderOnHudButton?.SetEnabled(true);
                        m_hideFolderOnHudButton?.SetEnabled(true);
                    }
                }
                else
                {
                    ___m_checkInsShowOnHud.Enabled = true;
                    ___m_checkInsAlwaysVisible.Enabled = true;

                    m_showFolderOnHudButton?.SetEnabled(false);
                    m_hideFolderOnHudButton?.SetEnabled(false);
                    if (m_gpsFolderNameTextBox != null)
                    {
                        if (sender.SelectedRow != null && sender.SelectedRow.TryGetFolderId(out string tag))
                            m_gpsFolderNameTextBox.Text = tag;
                        else
                            m_gpsFolderNameTextBox.Text = "";
                    }
                }
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "EnableEditBoxes", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(bool) })]
        static class Patch_EnableEditBoxes
        {
            static void Postfix(bool enable)
            {
                if (m_gpsFolderNameTextBox != null)
                    m_gpsFolderNameTextBox.Enabled = enable;
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "OnTableDoubleclick", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(MyGuiControlTable), typeof(MyGuiControlTable.EventArgs) })]
        static class Patch_OnTableDoubleclick
        {
            static bool Prefix(object __instance, MyGuiControlTable sender, MyGuiControlTable.EventArgs args, MyGuiControlSearchBox ___m_searchBox)
            {
                bool runOriginal = !(sender.SelectedRow is NonGpsRow);
                if (sender.SelectedRow is GpsFolderRow folder)
                {
                    if (currentFolderName == null)
                        currentFolderName = folder.Name;
                    else
                        currentFolderName = null;

                    PopulateList(__instance, ___m_searchBox.SearchText);
                    return false;
                }
                return runOriginal;
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "ToggleShowOnHud", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(MyGuiControlTable) })]
        static class Patch_ToggleShowOnHud
        {
            static bool Prefix(MyGuiControlTable sender)
            {
                bool runOriginal = !(sender.SelectedRow is NonGpsRow);
                return runOriginal;
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "OnNameChanged", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(MyGuiControlTextbox) })]
        static class Patch_OnNameChanged // called when the gps name field changes
        {
            public static bool Prefix(MyGuiControlTextbox sender, MyGuiControlTable ___m_tableIns)
            {
                if (___m_tableIns.SelectedRow == null)
                {
                    return false;
                }

                bool runOriginal = !(___m_tableIns.SelectedRow is NonGpsRow);
                MyGuiControlTable.Row selectedRow = ___m_tableIns.SelectedRow;

                if (selectedRow is GpsFolderRow folder)
                {
                    string oldFolderName = folder.Name;
                    string newFolderName = sender.Text;
                    if (gpsListView.TrySetFolderId(folder.Name, newFolderName))
                    {
                        if (oldFolderName == currentFolderName)
                        {
                            currentFolderName = newFolderName;
                        }

                        ___m_tableIns.Clear();
                        gpsListView.GetView(ref currentFolderName, gpsListView.LastSearchText, expandFoldersChecked).ForEach(row => ___m_tableIns.Add(row));

                        ___m_tableIns.SelectedRow = ___m_tableIns.Rows.FirstOrDefault(i => i is GpsFolderRow row && row.Name == newFolderName);
                        ___m_tableIns.ScrollToSelection();
                    }
                }

                return runOriginal;
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "OnButtonPressedCopy", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(MyGuiControlButton) })]
        static class Patch_OnButtonPressedCopy
        {
            static bool Prefix(MyGuiControlButton sender, MyGuiControlTable ___m_tableIns)
            {
                if (___m_tableIns.SelectedRow == null)
                    return false;

                bool runOriginal = !(___m_tableIns.SelectedRow is NonGpsRow);
                if (___m_tableIns.SelectedRow is GpsFolderRow folder)
                {
                    Helpers.CopyFolderToClipboard(folder.Name);
                    return false;
                }
                if (___m_tableIns.SelectedRow is UnsortedGpsFolderRow unsorted)
                {
                    Helpers.CopyUnsortedGpsesToClipboard();
                    return false;
                }
                else if (___m_tableIns.SelectedRow.TryGetFolderId(out string tag))
                {
                    MyVRage.Platform.System.Clipboard = ___m_tableIns.SelectedRow.UserData.ToString() + tag + ':';
                    return false;
                }

                return runOriginal;
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "Delete", MethodType.Normal)]
        [HarmonyPatch(new Type[] { })]
        static class Patch_Delete
        {
            static bool Prefix(MyGuiControlTable ___m_tableIns)
            {
                bool runOriginal = !(___m_tableIns.SelectedRow is NonGpsRow);
                if (___m_tableIns.SelectedRow is GpsFolderRow folder)
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
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "Close", MethodType.Normal)]
        [HarmonyPatch(new Type[] { })]
        static class Patch_Close
        {
            static void Postfix()
            {
                m_expandFoldersCheckbox = null;
                m_showFolderOnHudButton = null;
                m_hideFolderOnHudButton = null;
                m_gpsFolderNameTextBox = null;
                gpsListView = null;
                MiscellaneousPatches.m_showDistanceColumnCheckbox = null;
            }
        }

        //[HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "trySync", MethodType.Normal)]
        //[HarmonyPatch(new Type[] { })]
        //static class Patch_trySync
        //{
        //
        //}
    }
}
