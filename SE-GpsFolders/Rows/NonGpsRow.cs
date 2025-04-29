using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;

namespace GpsFolders.Rows
{
    abstract class NonGpsRow : MyGuiControlListbox.Item
    {
        public string Name { get; }
        public string DisplayName { get; }

        protected MyGps _dummyGps;

        protected NonGpsRow(string name, string displayName, Color color, MyGuiHighlightTexture? icon, string toolTip = null) : base(
            new StringBuilder(displayName),
            toolTip,
            icon?.Normal,
            new MyGps
            {
                Name = name,
                DisplayName = displayName,
                Description = "",
                IsLocal = true,
                AlwaysVisible = false,
                ShowOnHud = true,
                Coords = Vector3D.Zero,
                DiscardAt = null,
                GPSColor = color,
            }, 
            null)
        {
            Name = name;
            DisplayName = displayName;
            this.ColorMask = color;
            _dummyGps = (MyGps)UserData;
        }
    }
}
