using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace GpsFolders.Rows
{
    class UnsortedGpsFolderRow : NonGpsRow
    {
        public UnsortedGpsFolderRow(string name, string displayName, Color color, string toolTip = null)
            : base(name, displayName, color, null, toolTip)
        {

        }
    }
}
