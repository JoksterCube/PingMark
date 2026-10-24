using System;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.PingLabels;

[HarmonyPatch(typeof(Chat), nameof(Chat.UpdateWorldTextField))]
internal static class PingWorldTextLabelPatch
{
    private const string Feature = "Ping labels";

    private static void Postfix(Chat.WorldTextInstance __0)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { PostfixCore(__0); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void PostfixCore(Chat.WorldTextInstance __0)
    {
        Chat.WorldTextInstance worldText = __0;
        if (worldText.m_type != Talker.Type.Ping || !Enabled.IsOn()) return;

        Player localPlayer = Player.m_localPlayer;
        if (!localPlayer) return;

        PingLabelFormatter.Invalidate(worldText);
        worldText.m_textMeshField.text = PingLabelFormatter.Get(worldText, localPlayer);
    }
}