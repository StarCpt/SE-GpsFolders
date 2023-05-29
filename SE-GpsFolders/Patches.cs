using HarmonyLib;
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

namespace SE_GpsFolders
{
    abstract class NonGpsRow : MyGuiControlTable.Row
    {
        public string Name;
        public string DisplayName;
        public MyGuiHighlightTexture? Icon
        {
            get => this.GetCell(0).Icon;
            set => this.GetCell(0).Icon = value;
        }

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
            this.DisplayName = displayName;
            var cell = new MyGuiControlTable.Cell(
                this.DisplayName,
                this.UserData,
                toolTip,
                color,
                icon,
                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            this.AddCell(cell);
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
                MyGuiControlLabel expandFoldersLabel = new MyGuiControlLabel
                {
                    Position = new Vector2(-0.295f, -0.267f),
                    Name = "ExpandFoldersLabel",
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    Text = MyTexts.GetString("Expand Folders"),
                };

                MyGuiControlCheckbox expandFoldersCheckbox = new MyGuiControlCheckbox
                {
                    Position = new Vector2(-0.191f, -0.277f),
                    Name = "ExpandFoldersCheckbox",
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    IsChecked = MyTerminalGpsControllerPatches.expandFoldersChecked,
                };
                expandFoldersCheckbox.SetToolTip(new MyToolTips("Expand Folders"));

                gpsPage.Controls.Add(expandFoldersLabel);
                gpsPage.Controls.Add(expandFoldersCheckbox);
            }
        }
    }

    static class MyTerminalGpsControllerPatches
    {
        public const string MISC_GPS_SEPARATOR_NAME = "--------------------------------------------------";

        static readonly System.Reflection.MethodInfo populateListMethod = AccessTools.Method(
            "Sandbox.Game.Gui.MyTerminalGpsController:PopulateList", new Type[] { typeof(string) });

        public static string currentFolderTag = null;
        public static bool expandFoldersChecked = false;

        static void PopulateList(object instance, string searchString) => populateListMethod.Invoke(instance, new object[] { searchString });

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "Init", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(IMyGuiControlsParent) })]
        static class Patch_Init
        {
            static void Postfix(object __instance, IMyGuiControlsParent controlsParent, MyGuiControlSearchBox ___m_searchBox)
            {
                ((MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("ExpandFoldersCheckbox")).IsCheckedChanged += delegate
                {
                    expandFoldersChecked = !expandFoldersChecked;
                    PopulateList(__instance, ___m_searchBox.SearchText);
                };
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "PopulateList", MethodType.Normal)]
        [HarmonyPatch(new Type[] {typeof(string) })]
        public static class Patch_PopulateList
        {
            static void Postfix(string searchString, MyGuiControlTable ___m_tableIns)
            {
                var rowDict = new SortedDictionary<string, List<MyGuiControlTable.Row>>();
                for (int i = 0; i < ___m_tableIns.RowsCount; i++)
                {
                    var row = ___m_tableIns.Rows[i];
                    string folderTag = row.GetFolderTag();
                    if (folderTag != null)
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

                if (currentFolderTag == null)
                {
                    int rowIndex = 0;
                    bool addFolderChildren = expandFoldersChecked || !string.IsNullOrWhiteSpace(searchString);
                    foreach (var keyValue in rowDict)
                    {
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
                    var folder = new GpsFolderRow(currentFolderTag, '…' + currentFolderTag, Color.Yellow, MyGuiConstants.TEXTURE_ICON_BLUEPRINTS_LOCAL);
                    ___m_tableIns.Add(folder);
                    if (rowDict.ContainsKey(currentFolderTag))
                    {
                        folder.FolderSubRows = rowDict[currentFolderTag];
                        foreach (var row in rowDict[currentFolderTag])
                        {
                            ___m_tableIns.Add(row);
                        }
                    }
                }
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
                MyGuiControlButton ___m_buttonCopy)
            {
                if (sender.SelectedRow is NonGpsRow)
                {
                    ___m_panelInsName.Enabled = false;
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
                    ___m_buttonCopy.Enabled = false;

                    if (sender.SelectedRow is GpsFolderRow)
                    {
                        ___m_buttonCopy.Enabled = true;
                    }
                    else if (sender.SelectedRow is GpsSeparatorRow)
                    {

                    }
                }
                else
                {
                    ___m_checkInsShowOnHud.Enabled = true;
                    ___m_checkInsAlwaysVisible.Enabled = true;
                }
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
                    if (currentFolderTag == null)
                        currentFolderTag = folder.Name;
                    else
                        currentFolderTag = null;

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
            static void Postfix(MyGuiControlTable ___m_tableIns)
            {
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

                if (currentFolderTag != null)
                {
                    if (folderDict.TryGetValue(currentFolderTag, out GpsFolderRow folder))
                    {
                        folder.DisplayName = '…' + currentFolderTag;
                        folder.Icon = MyGuiConstants.TEXTURE_ICON_BLUEPRINTS_LOCAL;
                        ___m_tableIns.Insert(0, folder);
                    }
                }
                else
                {
                    foreach (var item in folderDict)
                    {
                        foreach (var row in item.Value.FolderSubRows)
                        {
                            ___m_tableIns.Remove(row);
                        }
                    }

                    int rowIndex = 0;
                    foreach (var item in folderDict)
                    {
                        ___m_tableIns.Insert(rowIndex, item.Value);
                        rowIndex++;
                        if (expandFoldersChecked)
                        {
                            foreach (var row in item.Value.FolderSubRows)
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

                ___m_tableIns.SelectedRow = selectedRow;
                if (___m_tableIns.SelectedRow is NonGpsRow)
                {
                    ___m_tableIns.SelectedRow = ___m_tableIns.Rows.FirstOrDefault(row => !(row is NonGpsRow));
                }
                ___m_tableIns.ScrollToSelection();
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "OnButtonPressedCopy", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(MyGuiControlButton) })]
        static class Patch_OnButtonPressedCopy
        {
            static bool Prefix(MyGuiControlButton sender, MyGuiControlTable ___m_tableIns)
            {
                bool runOriginal = !(___m_tableIns.SelectedRow is NonGpsRow);
                if (___m_tableIns.SelectedRow is GpsFolderRow folder)
                {
                    StringBuilder gpses = new StringBuilder();
                    for (int i = 0; i < folder.FolderSubRows.Count;)
                    {
                        gpses.Append(((MyGps)folder.FolderSubRows[i].UserData).ToString())
                             .Append(folder.Name)
                             .Append(":");

                        i++;
                        if (i == folder.FolderSubRows.Count - 1)
                            gpses.AppendLine();
                    }

                    if (gpses.Length > 0)
                    {
                        MyVRage.Platform.System.Clipboard = gpses.ToString();
                    }

                    return false;
                }
                else if (___m_tableIns.SelectedRow.GetFolderTag() is string tag)
                {
                    MyVRage.Platform.System.Clipboard = ___m_tableIns.SelectedRow.UserData.ToString() + tag + ':';
                    return false;
                }

                return runOriginal;
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "OnButtonPressedDelete", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(MyGuiControlButton) })]
        static class Patch_OnButtonPressedDelete
        {
            static bool Prefix(MyGuiControlButton sender, MyGuiControlTable ___m_tableIns)
            {
                bool runOriginal = !(___m_tableIns.SelectedRow is NonGpsRow);
                if (___m_tableIns.SelectedRow is GpsFolderRow folder)
                {
                    List<MyGps> gpsesToDelete = new List<MyGps>();

                    foreach (MyGuiControlTable.Row row in folder.FolderSubRows)
                    {
                        gpsesToDelete.Add((MyGps)row.UserData);
                    }

                    foreach (MyGps gps in gpsesToDelete)
                    {
                        MySession.Static.Gpss.SendDeleteGpsRequest(MySession.Static.LocalPlayerId, gps.GetHashCode());
                    }

                    return false;
                }
                return runOriginal;
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
            static readonly int PARSE_MAX_COUNT = 20;
            static readonly string m_ScanPattern = "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):";
            static readonly string m_FolderScanPattern = "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):([^:]{0,32}):";
            static readonly string m_ColorScanPattern = "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):(#[A-Fa-f0-9]{6}(?:[A-Fa-f0-9]{2})?):";
            static readonly string m_ColorAndFolderScanPattern = "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):(#[A-Fa-f0-9]{6}(?:[A-Fa-f0-9]{2})?):([^:]{0,32}):";

            static bool Prefix(string input, string desc, ref int __result)
            {
                int num = 0;
                bool containsColor = true;
                bool containsFolder = true;
                MatchCollection matchCollection = Regex.Matches(input, m_ColorAndFolderScanPattern);
                if (matchCollection == null || matchCollection.Count == 0)
                {
                    matchCollection = Regex.Matches(input, m_ColorScanPattern);
                    containsFolder = false;
                }
                if (matchCollection == null || matchCollection.Count == 0)
                {
                    matchCollection = Regex.Matches(input, m_ScanPattern);
                    containsColor = false;
                    containsFolder = false;
                }


                Color gPSColor = new Color(117, 201, 241);
                foreach (Match item in matchCollection)
                {
                    string value = item.Groups[1].Value;
                    double value2;
                    double value3;
                    double value4;
                    string folder = null;
                    try
                    {
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

    public static class Extensions
    {
        public static MyGuiHighlightTexture SetSize(this MyGuiHighlightTexture texture, Vector2 size)
        {
            texture.SizePx = size;
            return texture;
        }

        public static string GetFolderTag(this MyGuiControlTable.Row row)
        {
            if (!(row.UserData is NonGpsRow) && row.UserData is MyGps gps)
            {
                const string startTag = @"<Folder>";
                const string endTag = @"</Folder>";
                const int startIndex = 8;
                const int minTagLength = 1;
                const int maxTagLength = 32;
                int endIndex;
                var compareType = StringComparison.CurrentCulture;

                if (gps.Description.StartsWith(startTag, compareType) &&
                    (endIndex = gps.Description.IndexOf(endTag, startIndex, compareType)) != -1)
                {
                    string tag = gps.Description.Substring(startIndex, endIndex - startIndex);
                    if (tag.Length >= minTagLength && tag.Length <= maxTagLength)
                    {
                        return tag;
                    }
                }
            }
            return null;
        }
    }
}
