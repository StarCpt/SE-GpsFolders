using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;

namespace GpsFolders;

public static class Helpers
{
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

        result = MySession.Static.Gpss[MySession.Static.LocalPlayerId].Where(i => i.Value.TryGetFolderId(out string gpsFolderId) && gpsFolderId == folderId).Select(i => i.Value).ToArray();
        return result.Count() > 0;
    }

    public static void SetFolderShowOnHud(string? folderId, bool showOnHud)
    {
        IEnumerable<MyGps> gpses;
        if (folderId is null)
        {
            gpses = GetUnsortedGpses();
        }
        else if (!TryGetFolderGpses(folderId, out gpses))
        {
            return;
        }

        foreach (MyGps gps in gpses)
        {
            MySession.Static.Gpss.SendChangeShowOnHudRequest(MySession.Static.LocalPlayerId, gps.Hash, showOnHud);
        }
    }

    public static void SetFolderAlwaysVisible(string? folderId, bool alwaysVisible)
    {
        IEnumerable<MyGps> gpses;
        if (folderId is null)
        {
            gpses = GetUnsortedGpses();
        }
        else if (!TryGetFolderGpses(folderId, out gpses))
        {
            return;
        }

        foreach (MyGps gps in gpses)
        {
            MySession.Static.Gpss.SendChangeAlwaysVisibleRequest(MySession.Static.LocalPlayerId, gps.Hash, alwaysVisible);
        }
    }

    public static IEnumerable<MyGps> GetUnsortedGpses()
    {
        if (!MySession.Static.Gpss.ExistsForPlayer(MySession.Static.LocalPlayerId))
        {
            return Enumerable.Empty<MyGps>();
        }
        return MySession.Static.Gpss[MySession.Static.LocalPlayerId].Where(i => !i.Value.TryGetFolderId(out _)).Select(i => i.Value);
    }

    public static bool AllGpsesAreInSameFolder(IEnumerable<MyGps> gpses, out string? folder)
    {
        folder = gpses.First().GetFolderId();
        foreach (var gps in gpses)
        {
            if (gps.GetFolderId() != folder)
            {
                folder = null;
                return false;
            }
        }
        return true;
    }
}
