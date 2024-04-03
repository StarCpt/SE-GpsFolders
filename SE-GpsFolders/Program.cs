using HarmonyLib;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Plugins;
using VRage.Utils;

namespace GpsFolders
{
    public class Main : IPlugin
    {
        public void Init(object gameInstance)
        {
            new Harmony("GpsFolders").PatchAll(Assembly.GetExecutingAssembly());
        }

        public void Update()
        {

        }

        public void Dispose()
        {

        }

        public static void ShowErrorScreen(string message)
        {
            if (MySession.Static != null)
            {
                try
                {
                    MyAPIGateway.Utilities.ShowMissionScreen(
                        "Gps Folders: An error occurred",
                        null,
                        "Report the error to the plugin author!",
                        "You can contact me on the Plugin Loader discord.\nPress esc to ignore error.\n\n" + message,
                        result =>
                        {
                            if (result == VRage.Game.ModAPI.ResultEnum.OK)
                            {
                                MyVRage.Platform.System.Clipboard = message;
                            }
                        },
                        "Copy Error");
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLineAndConsole(e.ToString());
                }
            }
        }
    }
}
