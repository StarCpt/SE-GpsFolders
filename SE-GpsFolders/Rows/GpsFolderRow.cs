using Sandbox.Graphics.GUI;
using System.Collections.Generic;
using VRageMath;

namespace GpsFolders.Rows;

class GpsFolderRow : NonGpsRow
{
    public GpsFolderRow(string folderName, string displayName, Color color, MyGuiHighlightTexture? icon, string toolTip = null)
        : base(folderName, displayName, color, icon, toolTip)
    {
    }
}
