using System;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;

namespace JoksterCube.PingMark.Patches.Highlights;

[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.TestShow))]
internal static class MobHighlightVisibilityPatch
{
    private const string Feature = "Creature nameplates";

    private static void Postfix(Character __0, ref bool __result)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { PostfixCore(__0, ref __result); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void PostfixCore(Character __0, ref bool __result)
    {
        if (MobHighlight.IsMarked(__0)) __result = true;
    }
}