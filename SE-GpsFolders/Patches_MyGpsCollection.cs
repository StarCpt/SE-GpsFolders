using HarmonyLib;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VRage.Game;
using VRageMath;

namespace GpsFolders
{
    static class MyGpsCollectionPatches
    {
        [HarmonyPatch(typeof(MyGpsCollection), "ScanText", MethodType.Normal)]
        [HarmonyPatch(new Type[] { typeof(string), typeof(string) })]
        static class Patch_ScanText
        {
            static readonly int PARSE_MAX_COUNT = 100;
            static readonly string m_UnifiedScanPattern = @"GPS:([^:]{0,32}):([\d\.-]*):([\d\.-]*):([\d\.-]*):?(#[A-Fa-f0-9]{6}(?:[A-Fa-f0-9]{2})?)?:([^\r\n]{0,32}?):?(?=[^:]*GPS:|[^:]*$)";

            static bool Prefix(string input, string desc, ref int __result)
            {
                int num = 0;
                MatchCollection matchCollection = Regex.Matches(input, m_UnifiedScanPattern);

                foreach (Match item in matchCollection)
                {
                    string value = item.Groups[1].Value;
                    double value2;
                    double value3;
                    double value4;

                    Color gPSColor = new Color(117, 201, 241);
                    string folder = null;
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
                        }
                    }
                    catch (SystemException)
                    {
                        continue;
                    }

                    MyGps gps = new MyGps
                    {
                        Name = value,
                        Description = (folder != null ? $"<Folder>{folder}</Folder>\n{desc}" : desc),
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
    }
}
