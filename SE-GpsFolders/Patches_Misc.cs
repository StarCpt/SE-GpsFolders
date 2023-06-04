using HarmonyLib;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace GpsFoldersPlugin
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
                    if (row.UserData is MyGps gps)
                    {
                        row.GetCell(0).ToolTip.AddToolTip($"Distance: {Helpers.GetDistanceString(Vector3D.Distance(myPos, gps.Coords))}");
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
    }
}
