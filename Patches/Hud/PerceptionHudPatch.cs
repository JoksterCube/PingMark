using System;
using HarmonyLib;
using JoksterCube.AbilityKit;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.Hud;

// Draws the Perception and Fishsight slots after the Guardian Power, the same way vanilla draws the power.
[HarmonyPatch(typeof(global::Hud), nameof(global::Hud.UpdateGuardianPower))]
internal static class PerceptionHudPatch
{
    private const string Feature = "Perception HUD";

    private static void Postfix(global::Hud __instance)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try
        {
            // Before the slots, so they place themselves with the names already shown or hidden.
            AbilitySlot.SetNamesHidden(__instance, Settings.Constants.Plugin.ModName,
                Enabled.IsOn() && !ShowAbilityNames.IsOn());
            PerceptionHud.Update(__instance);
            HookedFish.UpdateHud(__instance);
        }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }
}
