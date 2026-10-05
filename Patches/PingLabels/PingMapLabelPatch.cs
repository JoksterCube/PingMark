using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.PingLabels;

[HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePingPins))]
internal static class PingMapLabelPatch
{
    private static readonly FieldInfo PinNameField = AccessTools.Field(typeof(Minimap.PinData), "m_name");

    private static void Postfix(List<Minimap.PinData> ___m_pingPins, List<Chat.WorldTextInstance> ___m_tempShouts)
    {
        if (!Enabled.IsOn()) return;

        Player localPlayer = Player.m_localPlayer;
        if (!localPlayer) return;

        int count = Mathf.Min(___m_pingPins.Count, ___m_tempShouts.Count);
        for (int i = 0; i < count; i++)
        {
            Chat.WorldTextInstance worldText = ___m_tempShouts[i];
            if (worldText.m_type != Talker.Type.Ping) continue;

            Minimap.PinData pingPin = ___m_pingPins[i];
            string label = PingMarkLabels.Get(worldText, localPlayer);
            PinNameField.SetValue(pingPin, label);

            if (pingPin.m_NamePinData != null && pingPin.m_NamePinData.PinNameText)
                pingPin.m_NamePinData.PinNameText.text = label;
        }
    }
}