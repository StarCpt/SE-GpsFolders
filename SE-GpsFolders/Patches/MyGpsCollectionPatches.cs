using HarmonyLib;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using VRage.Game;
using VRageMath;

namespace GpsFolders.Patches;

static class MyGpsCollectionPatches
{
    static readonly int PARSE_MAX_COUNT = 100;
    static readonly Regex m_UnifiedScanPattern = new(@"GPS:([^:]{0,32}):([\d\.-]*):([\d\.-]*):([\d\.-]*):?(#[A-Fa-f0-9]{6}(?:[A-Fa-f0-9]{2})?)?:([^\r\n]{0,32}?):?(?=[^:]*GPS:|[^:]*$)");

    [HarmonyPatch(typeof(MyGpsCollection), nameof(MyGpsCollection.ScanText))]
    [HarmonyPatch([typeof(string), typeof(string)])]
    [HarmonyPrefix]
    static bool ScanText_Prefix(string input, string desc, ref int __result)
    {
        int num = 0;
        MatchCollection matchCollection = m_UnifiedScanPattern.Matches(input);

        foreach (Match item in matchCollection)
        {
            string value = item.Groups[1].Value;
            double value2;
            double value3;
            double value4;

            Color gPSColor = new Color(117, 201, 241);
            string? folder = null;
            try
            {
                bool containsColor = !string.IsNullOrWhiteSpace(item.Groups[5].Value);
                bool containsFolder = !string.IsNullOrWhiteSpace(item.Groups[6].Value);

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
                    if (string.IsNullOrWhiteSpace(folder))
                    {
                        folder = null;
                    }
                }
            }
            catch (SystemException)
            {
                continue;
            }

            MyGps gps = new MyGps
            {
                Name = value,
                Description = (folder != null ? $"f:{folder}\n{desc}" : desc),
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
