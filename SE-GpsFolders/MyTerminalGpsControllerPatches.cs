using HarmonyLib;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace SE_GpsFolders
{
    class GpsFolder
    {
        public StringBuilder Name;
        public Color Color;

        public GpsFolder(StringBuilder name, Color color)
        {
            this.Name = name;
            this.Color = color;
        }
    }

    class GpsFolderRow : MyGuiControlTable.Row
    {
        public GpsFolderRow(object userData = null, string toolTip = null) : base(userData, toolTip)
        {

        }
    }

    internal class Patches
    {
        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "PopulateList", MethodType.Normal)]
        [HarmonyPatch(new Type[] {typeof(string) })]
        static class Patch_MyTerminalGpsController_PopulateList
        {
            static void Postfix(string searchString, MyGuiControlTable ___m_tableIns)
            {
                var folder = new GpsFolder(new StringBuilder("test folder"), Color.Green);
                GpsFolderRow row = new GpsFolderRow(folder, "tooltip test");
                row.AddCell(new MyGuiControlTable.Cell("folder cell 0", null, "cell tooltip", folder.Color));
                ___m_tableIns.Add(row);
            }
        }

        [HarmonyPatch("Sandbox.Game.Gui.MyTerminalGpsController", "OnTableItemSelected", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(MyGuiControlTable), typeof(MyGuiControlTable.EventArgs) })]
        static class Patch_MyTerminalGpsController_OnTableItemSelected
        {
            static readonly Type Type_MyTerminalGpsController = AccessTools.TypeByName("Sandbox.Game.Gui.MyTerminalGpsController");

            static readonly MethodInfo _enableEditBoxesMethod =
                Type_MyTerminalGpsController.GetMethod(
                    "EnableEditBoxes",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(bool) },
                    null);

            static readonly MethodInfo _setDeleteButtonEnabledMethod =
                Type_MyTerminalGpsController.GetMethod(
                    "SetDeleteButtonEnabled",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(bool) },
                    null);

            static readonly MethodInfo _clearRightMethod =
                Type_MyTerminalGpsController.GetMethod(
                    "ClearRight",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new Type[] { },
                    null);

            static bool Prefix(object __instance, MyGuiControlTable sender, MyGuiControlTable.EventArgs args, MyGuiControlTextbox ___m_panelInsName)
            {
                if (sender.SelectedRow?.UserData is GpsFolder folder)
                {
                    _enableEditBoxesMethod.Invoke(__instance, new object[] { false });
                    _setDeleteButtonEnabledMethod.Invoke(__instance, new object[] { false });
                    _clearRightMethod.Invoke(__instance, null);

                    ___m_panelInsName.SetText(folder.Name);
                    return false;
                }
                return true;
            }
        }
    }
}
