using HarmonyLib;
using Sandbox.Game.Screens.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using VRageMath;

namespace GpsFolders.Patches;

[HarmonyPatch]
public static class MyGpsPatches
{
    [HarmonyPatch(typeof(MyGps), nameof(MyGps.ConvertToString))]
    [HarmonyPatch([typeof(string), typeof(Vector3D), typeof(Color?)])]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ConvertToString_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // fixes big numbers becoming scientific notations (ex: 1e+16)
        // these gpses aren't detected when using copy from clipboard (regex pattern doesn't take it into account)

        // this patch does the following:
        // original: coords.X.ToString(CultureInfo.InvariantCulture);
        // patched: coords.X.ToString("0.################", CultureInfo.InvariantCulture);

        FieldInfo fx = AccessTools.DeclaredField(typeof(Vector3D), nameof(Vector3D.X));
        FieldInfo fy = AccessTools.DeclaredField(typeof(Vector3D), nameof(Vector3D.Y));
        FieldInfo fz = AccessTools.DeclaredField(typeof(Vector3D), nameof(Vector3D.Z));
        MethodInfo getInvariantCultureMethod = AccessTools.PropertyGetter(typeof(CultureInfo), nameof(CultureInfo.InvariantCulture));
        MethodInfo doubleToStringMethod = AccessTools.DeclaredMethod(typeof(double), nameof(double.ToString), [typeof(IFormatProvider)]);

        var instructions2 = instructions.ToList();
        for (int i = 0; i <= instructions2.Count - 4; i++)
        {
            var il0 = instructions2[i];
            var il1 = instructions2[i + 1];
            var il2 = instructions2[i + 2];

            if (il0.opcode == OpCodes.Ldflda && (il0.operand as FieldInfo == fx || il0.operand as FieldInfo == fy || il0.operand as FieldInfo == fz)
                && il1.opcode == OpCodes.Call && il1.operand.Equals(getInvariantCultureMethod)
                && il2.opcode == OpCodes.Call && il2.operand.Equals(doubleToStringMethod))
            {
                instructions2[i + 2].operand = AccessTools.DeclaredMethod(typeof(double), nameof(double.ToString), [typeof(string), typeof(IFormatProvider)]);
                instructions2.Insert(i + 1, new CodeInstruction(OpCodes.Ldstr, "0.################"));
                i += 4;
            }
        }
        return instructions2;
    }
}
