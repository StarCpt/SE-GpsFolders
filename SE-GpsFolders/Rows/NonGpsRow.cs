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
    abstract class NonGpsRow : MyGuiControlTable.Row
    {
        public string Name { get; protected set; }
        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    _cell.Text.Clear().Append(value);
                    _dummyGps.DisplayName = value;
                }
            }
        }
        public MyGuiHighlightTexture? Icon
        {
            get => _cell.Icon;
            set => _cell.Icon = value;
        }

        private string _displayName;
        protected MyGps _dummyGps;
        protected MyGuiControlTable.Cell _cell;

        protected NonGpsRow(string name, string displayName, Color color, MyGuiHighlightTexture? icon, string toolTip = null) : base(
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
            }, toolTip)
        {
            this.Name = name;
            this._displayName = displayName;
            this._dummyGps = (MyGps)this.UserData;
            this._cell = new MyGuiControlTable.Cell(
                this.DisplayName,
                this.UserData,
                toolTip,
                color,
                icon,
                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            this.AddCell(_cell);
        }
    }
}
