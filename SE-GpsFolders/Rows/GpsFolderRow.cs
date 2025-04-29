using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace GpsFolders.Rows
{
    class GpsFolderRow : NonGpsRow
    {
        public List<MyGuiControlListbox.Item> FolderSubRows; // gpses that would be visible if folders were expanded. takes the search string into account

        public GpsFolderRow(string folderName, string displayName, Color color, MyGuiHighlightTexture? icon, string toolTip = null)
            : base(folderName, displayName, color, icon, toolTip)
        {
            FolderSubRows = new List<MyGuiControlListbox.Item>();
        }
    }
}
