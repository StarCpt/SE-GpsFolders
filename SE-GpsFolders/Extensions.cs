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

        public static bool TryGetFolderId(this MyGuiControlTable.Row row, out string tag)
        {
            if (row != null && !(row.UserData is NonGpsRow) && row.UserData is MyGps gps)
            {
                return gps.TryGetFolderId(out tag);
            }
            tag = null;
            return false;
        }

        public static bool TryGetFolderId(this MyGps gps, out string folder)
        {
            return (folder = gps.GetFolderId()) != null;
        }

        public static string GetFolderId(this MyGps gps)
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
                if (IsFolderIdValid(tag))
                {
                    return tag;
                }
            }
            return null;
        }

        public static void SetFolderId(this MyGuiControlTable.Row row, string id)
        {
            if (row != null && !(row.UserData is NonGpsRow) && row.UserData is MyGps gps)
            {
                gps.SetFolderId(id);
            }
        }

        public static void SetFolderId(this MyGps gps, string id)
        {
            if (id != null && (IsFolderIdValid(id) || string.IsNullOrWhiteSpace(id)))
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

                if (IsFolderIdValid(id))
                {
                    gps.Description = startTag + id + endTag + (!gps.Description.StartsWith("\n") ? "\n" : "") + gps.Description;
                }

                MySession.Static.Gpss.SendModifyGpsRequest(MySession.Static.LocalPlayerId, gps);
            }
        }

        public static bool IsFolderIdValid(string folderName)
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

        public static void OrderedInsert<T>(this List<T> list, T value, Comparison<T> comparer)
        {
            int i = 0;
            for (; i < list.Count; i++)
            {
                if (comparer(value, list[i]) < 0)
                {
                    list.Insert(i, value);
                    return;
                }
            }
            
            if (i >= list.Count)
            {
                list.Add(value);
            }
        }
    }
}
