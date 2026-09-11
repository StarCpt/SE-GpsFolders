using HarmonyLib;
using Sandbox.Graphics.GUI;

namespace GpsFolders.Patches;

// fixes a crash in MyGuiControlMultilineText.Clear where text is cleared but RefreshText is not called unlike when setting Text = null
// where the selection indices are updated, so if the previous indices are not in the range of the new string, an ArgumentOutOfRangeException is thrown
// in either MyGuiControlMultilineText.DrawSelectionBackgrounds or the override in MyGuiControlMultilineEditableText.
[HarmonyPatch]
public static class MyGuiControlMultilineTextCrashFix
{
    [HarmonyPatch(typeof(MyGuiControlMultilineText), nameof(MyGuiControlMultilineText.Clear))]
    [HarmonyPostfix]
    public static void Clear_Postfix(MyGuiControlMultilineText __instance)
    {
        // fix carriage position
        // it's always 0 since we just cleared the text
        __instance.m_carriagePositionIndex = 0;

        // fix selection
        // also just zeroed since Reset sets both start and end indices to Clamp(carriagePos, 0, textLength)
        __instance.m_selection.Reset(__instance);

        if (__instance is MyGuiControlMultilineEditableText editable)
        {
            // emulate MyGuiControlMultilineEditableText.Text = null behavior
            editable.OnTextChanged();
            editable.RefreshText(false);
        }
    }
}
