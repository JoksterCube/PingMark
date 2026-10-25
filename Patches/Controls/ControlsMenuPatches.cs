using System;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using Valheim.SettingsGui;

namespace JoksterCube.PingMark.Patches.Controls;

// Ping and Perception rows in the game's Settings > Controls tab, kept in the config shortcuts.
internal static class ControlsMenuPatches
{
    private const string Feature = "Controls menu shortcuts";

    private static void Guard(Action action)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { action(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    [HarmonyPatch(typeof(KeyboardMouseSettings), nameof(KeyboardMouseSettings.Initialize))]
    private static class AddRowsPatch
    {
        private static void Prefix(KeyboardMouseSettings __instance) => Guard(() => ControlsMenu.AddRows(__instance));
    }

    [HarmonyPatch(typeof(KeyboardMouseSettings), nameof(KeyboardMouseSettings.UpdateBindings))]
    private static class UpdateLabelsPatch
    {
        private static void Postfix(KeyboardMouseSettings __instance) => Guard(() => ControlsMenu.UpdateLabels(__instance));
    }

    [HarmonyPatch(typeof(KeyboardMouseSettings), nameof(KeyboardMouseSettings.OnOkAsync))]
    private static class SavePatch
    {
        private static void Postfix() => Guard(ControlsMenu.Save);
    }

    [HarmonyPatch(typeof(KeyboardMouseSettings), nameof(KeyboardMouseSettings.ResetBindings))]
    private static class ResetPatch
    {
        private static void Postfix(KeyboardMouseSettings __instance) => Guard(() => ControlsMenu.Reset(__instance));
    }
}
