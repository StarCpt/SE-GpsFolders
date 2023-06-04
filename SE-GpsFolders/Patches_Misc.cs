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

namespace GpsFolders
{
    static class MiscellaneousPatches
    {
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
                        row.GetCell(0).ToolTip.AddToolTip($"Distance: {Helpers.GetDistanceString(Vector3D.Distance(myPos, gps.Coords))}");
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
            }

            private static void CopyAllGpsesToClipboardDialogCallback(MyGuiScreenMessageBox.ResultEnum result)
            {
                if (result == MyGuiScreenMessageBox.ResultEnum.YES && MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId))
                {
                    SortedDictionary<string, List<MyGps>> gpsDict = new SortedDictionary<string, List<MyGps>>();
                    StringBuilder strb = new StringBuilder();
                    
                    foreach (MyGps item in MySession.Static.Gpss[MySession.Static.LocalPlayerId].Values)
                    {
                        string tag = item.GetFolderTag() ?? string.Empty;
                        if (!gpsDict.ContainsKey(tag))
                        {
                            gpsDict.Add(tag, new List<MyGps>());
                        }
                        gpsDict[tag].Add(item);
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

    public static class Helpers
    {
        public static string GetDistanceString(double meters)
        {
            if (meters >= 1000000)
                return $"{meters / 1000:0.0} km";
            else if (meters >= 1000)
                return $"{meters / 1000:0.00} km";
            else
                return $"{meters:0.0} m";
        }

        public static void ShowConfirmationDialog(string caption, string text, Action<MyGuiScreenMessageBox.ResultEnum> callback)
        {
            var confirmationDialog = MyGuiSandbox.CreateMessageBox(
                MyMessageBoxStyleEnum.Info,
                MyMessageBoxButtonsType.YES_NO,
                new StringBuilder(text),
                new StringBuilder(caption),
                callback: callback);
            MyGuiSandbox.AddScreen(confirmationDialog);
        }
    }
}
