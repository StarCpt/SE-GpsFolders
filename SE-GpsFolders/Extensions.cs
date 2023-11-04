using GpsFolders;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace GpsFolders
{
    public static class Extensions
    {
        const int minTagLength = 1;
        const int maxTagLength = 32;

        public static MyGuiHighlightTexture SetSize(this MyGuiHighlightTexture texture, Vector2 size)
        {
            texture.SizePx = size;
            return texture;
        }

        public static bool TryGetFolderTag(this MyGuiControlTable.Row row, out string tag)
        {
            if (row != null && !(row.UserData is NonGpsRow) && row.UserData is MyGps gps)
            {
                tag = GetFolderTag(gps);
                return tag != null;
            }
            tag = null;
            return false;
        }

        public static string GetFolderTag(this MyGps gps)
        {
            const string startTag = @"<Folder>";
            const string endTag = @"</Folder>";
            const int startIndex = 8;
            int endIndex;
            var compareType = StringComparison.CurrentCulture;

            if (gps.Description != null &&
                    gps.Description.StartsWith(startTag, compareType) &&
                    (endIndex = gps.Description.IndexOf(endTag, startIndex, Math.Min(gps.Description.Length - startIndex, startIndex + maxTagLength + 1), compareType)) > startIndex)
            {
                string tag = gps.Description.Substring(startIndex, endIndex - startIndex);
                if (IsFolderNameValid(tag))
                {
                    return tag;
                }
            }
            return null;
        }

        public static void SetFolderTag(this MyGuiControlTable.Row row, string tag)
        {
            if (row != null && !(row.UserData is NonGpsRow) && !(row.UserData is GpsFolderRow) && row.UserData is MyGps gps && tag != null && (IsFolderNameValid(tag) || string.IsNullOrWhiteSpace(tag)))
            {
                const string startTag = @"<Folder>";
                const string endTag = @"</Folder>";
                const int startIndex = 8;
                int endIndex;
                var compareType = StringComparison.CurrentCulture;

                if (gps.Description != null &&
                    gps.Description.StartsWith(startTag, compareType) &&
                    (endIndex = gps.Description.IndexOf(endTag, startIndex, Math.Min(gps.Description.Length - startIndex, startIndex + maxTagLength + 1), compareType)) > startIndex)
                {
                    gps.Description = gps.Description.Remove(0, endIndex + endTag.Length);
                }

                if (IsFolderNameValid(tag))
                {
                    gps.Description = startTag + tag + endTag + (!gps.Description.StartsWith("\n") ? "\n" : "") + gps.Description;
                }

                MySession.Static.Gpss.SendModifyGpsRequest(MySession.Static.LocalPlayerId, gps);
            }
        }

        public static bool IsFolderNameValid(string folderName)
        {
            return !string.IsNullOrWhiteSpace(folderName) &&
                folderName.Length >= minTagLength &&
                folderName.Length <= maxTagLength &&
                !folderName.EndsWith("GPS") &&
                !folderName.Contains(":");
        }

        public static void SetEnabled(this MyGuiControlBase control, bool enabled)
        {
            if (control != null)
            {
                control.Enabled = enabled;
            }
        }
    }
}
