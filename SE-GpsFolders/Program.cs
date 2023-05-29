using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Plugins;

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
    }
}
