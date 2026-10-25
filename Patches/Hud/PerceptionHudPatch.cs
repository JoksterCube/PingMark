using System;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;

namespace JoksterCube.PingMark.Patches.Hud;

// Draws the Perception slot right after the Guardian Power, the same way vanilla draws the power.
[HarmonyPatch(typeof(global::Hud), nameof(global::Hud.UpdateGuardianPower))]
internal static class PerceptionHudPatch
{
    private const string Feature = "Perception HUD";

    private static void Postfix(global::Hud __instance)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { PerceptionHud.Update(__instance); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
