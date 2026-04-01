using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace GpsFolders;

public class CustomIndeterminateCheckbox : MyGuiControlIndeterminateCheckbox
{
    public bool SkipIndeterminateState { get; set; }

    public CustomIndeterminateCheckbox(
        Vector2? position = null,
        Vector4? color = null,
        string? toolTip = null,
        CheckStateEnum state = CheckStateEnum.Unchecked,
        MyGuiControlIndeterminateCheckboxStyleEnum visualStyle = MyGuiControlIndeterminateCheckboxStyleEnum.Default,
        MyGuiDrawAlignEnum originAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER)
        : base(position, color, toolTip, state, visualStyle, originAlign)
    {
        IsCheckedChanged += _ => OnIsCheckedChanged();
    }

    private void OnIsCheckedChanged()
    {
        if (State is CheckStateEnum.Indeterminate && SkipIndeterminateState)
        {
            State = CheckStateEnum.Unchecked;
        }
    }
}
