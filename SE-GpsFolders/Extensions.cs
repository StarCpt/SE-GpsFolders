using GpsFolders.Rows;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Screens.Terminal;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace GpsFolders;

public static class Extensions
{
    private const int minTagLength = 1;
    private const int maxTagLength = 32;

    private const string startTagOld = @"<Folder>";
    private const string endTagOld = @"</Folder>";
    private static readonly int startIndexOld = startTagOld.Length;

    private const string startTag = @"f:";
    private const string endTag = "\n";
    private static readonly int startIndex = startTag.Length;

    public static bool TryGetFolderId(this MyGuiControlListbox.Item row, out string tag)
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
        if ((folder = gps.GetFolderIdInternal()) != null)
            return true;
        return (folder = gps.GetFolderIdOld()) != null;
    }

    public static string? GetFolderId(this MyGps gps)
    {
        if (TryGetFolderId(gps, out string folder))
        {
            return folder;
        }
        return null;
    }

    private static string GetFolderIdOld(this MyGps gps)
    {
        int endIndex;
        if (gps.Description != null &&
            gps.Description.StartsWith(startTagOld) &&
            (endIndex = gps.Description.IndexOf(endTagOld, startIndexOld, Math.Min(gps.Description.Length - startIndexOld, startIndexOld + maxTagLength + 1))) > startIndexOld)
        {
            string tag = gps.Description.Substring(startIndexOld, endIndex - startIndexOld);
            if (IsFolderIdValid(tag))
            {
                return tag;
            }
        }
        return null;
    }

    private static string GetFolderIdInternal(this MyGps gps)
    {
        if (gps.Description != null &&
            gps.Description.StartsWith(startTag))
        {
            int endIndex =
                gps.Description.Contains('\n') ?
                gps.Description.IndexOf(endTag, startIndex, Math.Min(gps.Description.Length - startIndex, startIndex + maxTagLength + 1)) :
                gps.Description.Length;
            string tag = gps.Description.Substring(startIndex, endIndex - startIndex);
            if (IsFolderIdValid(tag))
            {
                return tag;
            }
        }
        return null;
    }

    public static void SetFolderId(this MyGps gps, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = null;
        }

        if (IsFolderIdValid(id) || id is null)
        {
            // remove folder
            if (gps.Description is not null)
            {
                if (gps.Description.StartsWith(startTag))
                {
                    int endIndex =
                        gps.Description.Contains(endTag) ?
                        gps.Description.IndexOf(endTag, startIndex, Math.Min(gps.Description.Length - startIndex, startIndex + maxTagLength + 1)) :
                        gps.Description.Length;
                    if (endIndex > startIndex)
                    {
                        gps.Description = gps.Description.Remove(0, endIndex + endTag.Length);
                    }
                }
                else if (gps.Description.StartsWith(startTagOld))
                {
                    int endIndex = gps.Description.IndexOf(endTagOld, startIndexOld, Math.Min(gps.Description.Length - startIndexOld, startIndexOld + maxTagLength + 1));
                    if (endIndex > startIndexOld)
                    {
                        gps.Description = gps.Description.Remove(0, endIndex + endTagOld.Length);
                    }
                }
            }

            // add new folder
            if (IsFolderIdValid(id))
            {
                gps.Description ??= "";
                gps.Description = startTag + id + endTag + gps.Description;
            }

            MySession.Static.Gpss.SendModifyGpsRequest(MySession.Static.LocalPlayerId, gps);
        }
    }

    public static bool IsFolderIdValid(string? folderName)
    {
        return !string.IsNullOrWhiteSpace(folderName) &&
            folderName!.Length >= minTagLength &&
            folderName.Length <= maxTagLength &&
            !folderName.EndsWith("GPS") &&
            !folderName.Contains(":");
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

    public static void ClearRightExceptName(this MyTerminalGpsController controller, string gpsNameTextboxText, bool clearMultiSelectValues = true)
    {
        controller.UnhookSyncEvents(clearMultiSelectValues);
        controller.m_panelGpsDesc.Clear();
        controller.m_xCoord.Text = "";
        controller.m_yCoord.Text = "";
        controller.m_zCoord.Text = "";
        if (clearMultiSelectValues)
        {
            controller.m_textBoxHex.Text = "";
            controller.m_sliderValue.Value = 0f;
            controller.m_sliderSaturation.Value = 0f;
            controller.m_sliderHue.Value = 0f;
            controller.m_checkGpsShowOnHud.IsChecked = false;
            controller.m_checkGpsAlwaysVisible.IsChecked = false;
        }
        controller.m_previousHash = null;
        controller.RefreshGpsColorControlsTooltips(clear: true);
        controller.HookSyncEvents(clearMultiSelectValues);
        controller.m_needsSyncName = false;
        controller.m_needsSyncDesc = false;
        controller.m_needsSyncX = false;
        controller.m_needsSyncY = false;
        controller.m_needsSyncZ = false;
    }

    public static int Findindex<T>(this IReadOnlyList<T> list, Func<T, bool> predicate)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                return i;
            }
        }
        return -1;
    }
}
