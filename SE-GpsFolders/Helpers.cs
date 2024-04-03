using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;

namespace GpsFolders
{
    public static class Helpers
    {
        public static string GetDistanceString(double meters)
        {
            if (meters >= 1000000)
                return $"{meters / 1000:0.#} km";
            else if (meters >= 1000)
                return $"{meters / 1000:0.##} km";
            else
                return $"{meters:0.#} m";
        }

        public static string GetDistanceStringShort(double meters)
        {
            if (meters >= 1000000)
                return $"{meters / 1000000:0.#}kkm";
            else if (meters >= 1000)
                return $"{meters / 1000:0}km";
            else
                return $"{meters:0}m";
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

        public static bool TryGetFolderGpses(string folderId, out IEnumerable<MyGps> result)
        {
            if (!Extensions.IsFolderIdValid(folderId))
            {
                result = null;
                return false;
            }

            if (MySession.Static == null || !MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId))
            {
                result = null;
                return false;
            }

            result = MySession.Static.Gpss[MySession.Static.LocalPlayerId].Where(i => i.Value.GetFolderId() == folderId).Select(i => i.Value).ToArray();
            return result.Count() > 0;
        }

        public static void SetFolderShowOnHud(string folderId, bool showOnHud)
        {
            if (!TryGetFolderGpses(folderId, out IEnumerable<MyGps> gpses))
            {
                return;
            }

            foreach (MyGps gps in gpses)
            {
                if (gps.GetFolderId() == folderId)
                {
                    gps.ShowOnHud = showOnHud;
                    MySession.Static.Gpss.SendChangeShowOnHudRequest(MySession.Static.LocalPlayerId, gps.Hash, showOnHud);
                }
            }
        }

        public static void CopyFolderToClipboard(string folderId)
        {
            if (!TryGetFolderGpses(folderId, out IEnumerable<MyGps> gpses))
            {
                return;
            }

            string gpsStr = String.Join("\n", gpses.OrderBy(gps => gps.Name).Select(gps => $"{gps.ToString()}{folderId}:"));
            MyVRage.Platform.System.Clipboard = gpsStr.ToString();
        }
    }
}
