using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Game;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRage;
using VRageMath;
using GpsFolders.Rows;

namespace GpsFolders
{
    static class MiscellaneousPatches
    {
        public static MyGuiControlCheckbox m_showDistanceColumnCheckbox;
        public static bool showDistanceColumn = false;

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "Init", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(IMyGuiControlsParent) })]
        static class MyTerminalGpsController_Init
        {
            static void Postfix(object __instance, IMyGuiControlsParent controlsParent, MyGuiControlSearchBox ___m_searchBox, MyGuiControlTable ___m_tableIns)
            {
                (m_showDistanceColumnCheckbox = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("ShowDistanceCheckbox")).IsCheckedChanged += delegate
                {
                    showDistanceColumn = !showDistanceColumn;
                    ___m_tableIns.SetColumnVisibility(1, showDistanceColumn);
                    ___m_tableIns.Size = new Vector2(showDistanceColumn ? 0.3275f : 0.29f, 0.5f);
                    ___m_tableIns.PositionX = showDistanceColumn ? -0.47075f : -0.452f;
                };
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "PopulateList", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(string) })]
        static class MyTerminalGpsController_PopulateList
        {
            static void Postfix(MyGuiControlTable ___m_tableIns)
            {
                if (MySession.Static?.LocalCharacter?.PositionComp == null)
                    return;

                Vector3D myPos = MySession.Static.LocalCharacter.PositionComp.GetPosition();

                foreach (var row in ___m_tableIns.Rows)
                {
                    if (!(row is NonGpsRow) && row.UserData is MyGps gps)
                    {
                        double dist = Vector3D.Distance(myPos, gps.Coords);
                        row.GetCell(0).ToolTip.AddToolTip($"Distance: {Helpers.GetDistanceString(dist)}");
                        row.AddCell(new MyGuiControlTable.Cell(Helpers.GetDistanceStringShort(dist), null, $"Distance: {Helpers.GetDistanceString(dist)}"));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MyGuiScreenTerminal), "CreateGpsPageControls", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(MyGuiControlTabPage) })]
        static class MyGuiScreenTerminal_CreateGpsPageControls
        {
            static void Postfix(MyGuiControlTabPage gpsPage)
            {
                MyGuiControlBase textInsY = gpsPage.GetControlByName("textInsY");
                MyGuiControlBase buttonFromCurrent = gpsPage.GetControlByName("buttonFromCurrent");
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

                MyGuiControlTable tableIns = (MyGuiControlTable)gpsPage.Controls.GetControlByName("TableINS");
                tableIns.ColumnsCount = 2;
                tableIns.SetCustomColumnWidths(new float[2] { 0.75f, 0.25f });
                tableIns.SetColumnAlign(1, MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER);
                tableIns.SetColumnVisibility(1, showDistanceColumn);
                tableIns.Size = new Vector2(showDistanceColumn ? 0.3275f : 0.29f, 0.5f);
                tableIns.PositionX = showDistanceColumn ? -0.47075f : -0.452f;

                MyGuiControlLabel expandFoldersLabel = new MyGuiControlLabel
                {
                    Position = new Vector2(-0.321f, -0.267f + 0.011f),
                    Name = "ShowDistanceLabel",
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
                    Text = MyTexts.GetString("Dist"),
                    TextScale = 0.7f,
                };
                gpsPage.Controls.Add(expandFoldersLabel);

                MyGuiControlCheckbox expandFoldersCheckbox = new MyGuiControlCheckbox
                {
                    Position = new Vector2(-0.316f, -0.277f),
                    Name = "ShowDistanceCheckbox",
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                    IsChecked = showDistanceColumn,
                };
                expandFoldersCheckbox.SetToolTip(new MyToolTips("Show Distance Column"));
                gpsPage.Controls.Add(expandFoldersCheckbox);
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
    }
}
