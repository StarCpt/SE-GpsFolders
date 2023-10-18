using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace GpsFolders
{
    abstract class NonGpsRow : MyGuiControlTable.Row
    {
        public string Name { get; protected set; }
        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    _cell.Text.Clear().Append(value);
                    _dummyGps.DisplayName = value;
                }
            }
        }
        public MyGuiHighlightTexture? Icon
        {
            get => _cell.Icon;
            set => _cell.Icon = value;
        }

        private string _displayName;
        protected MyGps _dummyGps;
        protected MyGuiControlTable.Cell _cell;

        protected NonGpsRow(string name, string displayName, Color color, MyGuiHighlightTexture? icon, string toolTip = null) : base(
            new MyGps
            {
                Name = name,
                DisplayName = displayName,
                Description = "",
                IsLocal = true,
                AlwaysVisible = false,
                ShowOnHud = true,
                Coords = Vector3D.Zero,
                DiscardAt = null,
                GPSColor = color,
            }, toolTip)
        {
            this.Name = name;
            this._displayName = displayName;
            this._dummyGps = (MyGps)this.UserData;
            this._cell = new MyGuiControlTable.Cell(
                this.DisplayName,
                this.UserData,
                toolTip,
                color,
                icon,
                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            this.AddCell(_cell);
        }
    }
    class GpsFolderRow : NonGpsRow
    {
        public List<MyGuiControlTable.Row> FolderSubRows;//gpses that would be visible if folders were expanded. takes the search string into account

        public GpsFolderRow(string folderName, string displayName, Color color, MyGuiHighlightTexture? icon, string toolTip = null)
            : base(folderName, displayName, color, icon, toolTip)
        {
            FolderSubRows = new List<MyGuiControlTable.Row>();
        }

        public void SetName(string name)
        {
            foreach (var row in FolderSubRows)
            {
                row.SetFolderTag(name);
            }
            this.Name = name;
            _dummyGps.Name = name;
        }

        public void SetFolderGpsVisibilities(bool showOnHud)
        {
            foreach (MyGps gps in MySession.Static.Gpss[MySession.Static.LocalPlayerId].Values)
            {
                if (gps.GetFolderTag() == this.Name)
                {
                    gps.ShowOnHud = showOnHud;
                    MySession.Static.Gpss.SendChangeShowOnHudRequest(MySession.Static.LocalPlayerId, gps.Hash, showOnHud);
                }
            }
        }

        public List<MyGps> GetFolderGpses()
        {
            return MySession.Static.Gpss[MySession.Static.LocalPlayerId].Values.Where(i => i.GetFolderTag() == this.Name).ToList();
        }

        public void CopyToClipboard()
        {
            var gpses = this.GetFolderGpses();
            if (gpses.Count > 0)
            {
                string gpsStr = String.Join("\n", gpses.OrderBy(gps => gps.Name).Select(gps => $"{gps.ToString()}{this.Name}:"));
                MyVRage.Platform.System.Clipboard = gpsStr.ToString();
            }
        }

        public void DeleteFolderGpses()
        {
            List<MyGps> gpsesToDelete = this.GetFolderGpses();

            Helpers.ShowConfirmationDialog(
                "Delete Folder",
                "Are you sure you want to delete this folder and its contents?",
                result =>
                {
                    if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                    {
                        foreach (MyGps gps in gpsesToDelete)
                        {
                            MySession.Static.Gpss.SendDeleteGpsRequest(MySession.Static.LocalPlayerId, gps.GetHashCode());
                        }
                    }
                });
        }
    }

    class GpsSeparatorRow : NonGpsRow
    {
        public GpsSeparatorRow(string name, string displayName, Color color, string toolTip = null)
            : base(name, displayName, color, null, toolTip)
        {

        }
    }

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

        static readonly System.Reflection.MethodInfo populateListMethod = AccessTools.Method(
            "Sandbox.Game.Gui.MyTerminalGpsController:PopulateList", new Type[] { typeof(string) });

        public static string currentFolderName = null;
        public static bool expandFoldersChecked = false;

        static MyGuiControlCheckbox m_expandFoldersCheckbox;
        static MyGuiControlButton m_showFolderOnHudButton;
        static MyGuiControlButton m_hideFolderOnHudButton;
        static MyGuiControlTextbox m_gpsFolderNameTextBox;

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
                        folder.SetFolderGpsVisibilities(true);
                    }
                };

                (m_hideFolderOnHudButton = (MyGuiControlButton)controlsParent.Controls.GetControlByName("HideFolderOnHudButton")).ButtonClicked += delegate
                {
                    if (___m_tableIns.SelectedRow is GpsFolderRow folder)
                    {
                        folder.SetFolderGpsVisibilities(false);
                    }
                };

                (m_gpsFolderNameTextBox = (MyGuiControlTextbox)controlsParent.Controls.GetControlByName("GpsFolderNameTextBox")).TextChanged += textbox =>
                {
                    ___m_tableIns.SelectedRow?.SetFolderTag(textbox.Text);
                };
                m_gpsFolderNameTextBox.Enabled = false;
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "PopulateList", MethodType.Normal)]
        [HarmonyPatch(new Type[] {typeof(string) })]
        public static class Patch_PopulateList
        {
            static void Postfix(string searchString, object __instance, MyGuiControlTable ___m_tableIns)
            {
                var rowDict = new SortedDictionary<string, List<MyGuiControlTable.Row>>();
                for (int i = 0; i < ___m_tableIns.RowsCount; i++)
                {
                    var row = ___m_tableIns.Rows[i];
                    string folderTag;
                    if (row.TryGetFolderTag(out folderTag))
                    {
                        if (!rowDict.ContainsKey(folderTag))
                        {
                            rowDict.Add(folderTag, new List<MyGuiControlTable.Row>());
                        }

                        ___m_tableIns.Remove(row);
                        i--;
                        rowDict[folderTag].Add(row);
                    }
                }

                if (currentFolderName == null)
                {
                    int rowIndex = 0;
                    foreach (var keyValue in rowDict)
                    {
                        bool addFolderChildren = expandFoldersChecked || !string.IsNullOrWhiteSpace(searchString);
                        var folder = new GpsFolderRow(keyValue.Key, keyValue.Key, Color.Yellow, MyGuiConstants.TEXTURE_ICON_MODS_LOCAL);
                        ___m_tableIns.Insert(rowIndex, folder);
                        rowIndex++;

                        folder.FolderSubRows = keyValue.Value;
                        if (addFolderChildren)
                        {
                            foreach (var row in keyValue.Value)
                            {
                                ___m_tableIns.Insert(rowIndex, row);  
                                
                                rowIndex++;
                            }
                        }
                    }

                    if (rowIndex > 0)
                    {
                        GpsSeparatorRow separatorRow = new GpsSeparatorRow(MISC_GPS_SEPARATOR_NAME, MISC_GPS_SEPARATOR_NAME, Color.Yellow, null);
                        ___m_tableIns.Insert(rowIndex, separatorRow);
                        rowIndex++;
                    }
                }
                else
                {
                    ___m_tableIns.Rows.Clear();
                    var folder = new GpsFolderRow(currentFolderName, '…' + currentFolderName, Color.Yellow, MyGuiConstants.TEXTURE_ICON_BLUEPRINTS_LOCAL);
                    ___m_tableIns.Add(folder);
                    if (rowDict.ContainsKey(currentFolderName))
                    {
                        folder.FolderSubRows = rowDict[currentFolderName];
                        foreach (var row in rowDict[currentFolderName])
                        {
                            ___m_tableIns.Add(row);
                        }
                    }
                }

                if (___m_tableIns.SelectedRow is NonGpsRow && !(___m_tableIns.SelectedRow is GpsFolderRow))
                {
                    ___m_tableIns.SelectedRowIndex = null;
                }

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
                    else if (sender.SelectedRow is GpsSeparatorRow)
                    {
                        ___m_panelInsName.Enabled = false;
                        ___m_buttonCopy.Enabled = false;

                        m_showFolderOnHudButton?.SetEnabled(false);
                        m_hideFolderOnHudButton?.SetEnabled(false);
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
                        if (sender.SelectedRow != null && sender.SelectedRow.TryGetFolderTag(out string tag))
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
        static class Patch_OnNameChanged
        {
            static bool Prefix(MyGuiControlTextbox sender, MyGuiControlTable ___m_tableIns)
            {
                if (___m_tableIns.SelectedRow == null)
                    return false;

                bool runOriginal = !(___m_tableIns.SelectedRow is NonGpsRow);
                MyGuiControlTable.Row selectedRow = ___m_tableIns.SelectedRow;

                if (selectedRow is GpsFolderRow folder && Extensions.IsFolderNameValid(sender.Text))
                {
                    folder.SetName(sender.Text);
                    folder.DisplayName = '…' + folder.Name;
                    currentFolderName = folder.Name;
                    ___m_tableIns.SelectedRow = selectedRow;
                    ___m_tableIns.ScrollToSelection();
                }

                return runOriginal;
            }

            static void Postfix(MyGuiControlTable ___m_tableIns)
            {
                if (___m_tableIns.SelectedRow == null)
                    return;

                MyGuiControlTable.Row selectedRow = ___m_tableIns.SelectedRow;

                var folderDict = new SortedDictionary<string, GpsFolderRow>();

                for (int i = 0; i < ___m_tableIns.RowsCount; i++)
                {
                    if (___m_tableIns.Rows[i] is NonGpsRow row)
                    {
                        ___m_tableIns.Remove(row);
                        i--;
                        if (row is GpsFolderRow folder)
                        {
                            folderDict.Add(folder.Name, folder);
                        }
                    }
                }

                if (currentFolderName != null)
                {
                    if (folderDict.TryGetValue(currentFolderName, out GpsFolderRow folder))
                    {
                        folder.DisplayName = '…' + folder.Name;
                        folder.Icon = MyGuiConstants.TEXTURE_ICON_BLUEPRINTS_LOCAL;
                        ___m_tableIns.Insert(0, folder);
                    }
                }
                else
                {
                    int rowIndex = 0;
                    foreach (var item in folderDict)
                    {
                        ___m_tableIns.Insert(rowIndex, item.Value);
                        rowIndex++;
                        if (expandFoldersChecked)
                        {
                            foreach (var row in item.Value.FolderSubRows)
                            {
                                ___m_tableIns.Remove(row);
                                ___m_tableIns.Insert(rowIndex, row);
                                rowIndex++;
                            }
                        }
                    }

                    if (rowIndex > 0)
                    {
                        GpsSeparatorRow separatorRow = new GpsSeparatorRow(MISC_GPS_SEPARATOR_NAME, MISC_GPS_SEPARATOR_NAME, Color.Yellow, null);
                        ___m_tableIns.Insert(rowIndex, separatorRow);
                        rowIndex++;
                    }
                }

                ___m_tableIns.SelectedRow = selectedRow;
                if (___m_tableIns.SelectedRow is NonGpsRow && !(___m_tableIns.SelectedRow is GpsFolderRow))
                {
                    ___m_tableIns.SelectedRowIndex = null;
                }
                else
                {
                    ___m_tableIns.ScrollToSelection();
                }
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
                    folder.CopyToClipboard();

                    return false;
                }
                else if (___m_tableIns.SelectedRow.TryGetFolderTag(out string tag))
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
                    folder.DeleteFolderGpses();

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
            }
        }

        //[HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "trySync", MethodType.Normal)]
        //[HarmonyPatch(new Type[] { })]
        //static class Patch_trySync
        //{
        //
        //}
    }

    static class MyGpsCollectionPatches
    {
        [HarmonyPatch(typeof(MyGpsCollection), "ScanText", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(string), typeof(string) })]
        static class Patch_ScanText
        {
            static readonly int PARSE_MAX_COUNT = 100;
            static readonly string m_UnifiedScanPattern = @"GPS:([^:]{0,32}):([\d\.-]*):([\d\.-]*):([\d\.-]*):?(#[A-Fa-f0-9]{6}(?:[A-Fa-f0-9]{2})?)?:([^\r\n]{0,32}?):?(?=[^:]*GPS:|[^:]*$)";

            static bool Prefix(string input, string desc, ref int __result)
            {
                int num = 0;
                MatchCollection matchCollection = Regex.Matches(input, m_UnifiedScanPattern);

                foreach (Match item in matchCollection)
                {
                    string value = item.Groups[1].Value;
                    double value2;
                    double value3;
                    double value4;

                    Color gPSColor = new Color(117, 201, 241);
                    string folder = null;
                    try
                    {
                        bool containsColor = !string.IsNullOrWhiteSpace(item.Groups[5].Value);
                        bool containsFolder = !string.IsNullOrWhiteSpace(item.Groups[6].Value);

                        value2 = double.Parse(item.Groups[2].Value, CultureInfo.InvariantCulture);
                        value2 = Math.Round(value2, 2);
                        value3 = double.Parse(item.Groups[3].Value, CultureInfo.InvariantCulture);
                        value3 = Math.Round(value3, 2);
                        value4 = double.Parse(item.Groups[4].Value, CultureInfo.InvariantCulture);
                        value4 = Math.Round(value4, 2);
                        if (containsColor)
                        {
                            gPSColor = new ColorDefinitionRGBA(item.Groups[5].Value);
                        }
                        if (containsFolder)
                        {
                            folder = item.Groups[6].Value;
                        }
                    }
                    catch (SystemException)
                    {
                        continue;
                    }

                    MyGps gps = new MyGps
                    {
                        Name = value,
                        Description = (folder!= null ? $"<Folder>{folder}</Folder>\n{desc}" : desc),
                        Coords = new Vector3D(value2, value3, value4),
                        GPSColor = gPSColor,
                        ShowOnHud = false
                    };
                    gps.UpdateHash();
                    MySession.Static.Gpss.SendAddGpsRequest(MySession.Static.LocalPlayerId, ref gps, 0L);
                    num++;
                    if (num == PARSE_MAX_COUNT)
                    {
                        __result = num;
                    }
                }

                __result = num;
                return false;
            }
        }
    }
}
