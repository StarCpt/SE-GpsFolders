using HarmonyLib;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.Screens.Terminal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace GpsFolders.Patches;

// fix the delete button being disabled for readonly gpses
// except for contract gpses, they're needed to complete contracts
[HarmonyPatch]
public static class GpsDeleteButtonPatch
{
	[HarmonyTargetMethods]
	public static IEnumerable<MethodBase> TargetMethods()
	{
        yield return AccessTools.DeclaredMethod(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.SetEnabledStates)) ?? throw new Exception("Target method not found!");
        yield return AccessTools.DeclaredMethod(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.Delete)) ?? throw new Exception("Target method not found!");
	}

    [HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
        MethodInfo targetMethod = AccessTools.DeclaredMethod(typeof(MyTerminalGpsController), nameof(MyTerminalGpsController.IsReadOnlyGps)) ?? throw new Exception("Target method not found!");

        foreach (var il in instructions)
        {
            if (il.opcode == OpCodes.Call && il.operand as MethodInfo == targetMethod)
            {
                il.operand = ((Delegate)Helpers.IsContractGps).Method;
			}
        }
        return instructions;
    }
}
