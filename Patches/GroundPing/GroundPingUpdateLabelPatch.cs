using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using JoksterCube.PingMark.Domain.Collections;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.GroundPing;

[HarmonyPatch(typeof(Chat), nameof(Chat.UpdateWorldTexts))]
internal static class GroundPingUpdateLabelPatch
{
    private static readonly List<Chat.WorldTextInstance> Expired = new();

    private static void Prefix(float dt)
    {
        if (!Enabled.IsOn())
        {
            GroundPingLabelAnchors.Anchors.Clear();
            return;
        }

        foreach (KeyValuePair<Chat.WorldTextInstance, Vector3> entry in GroundPingLabelAnchors.Anchors)
        {
            if (!entry.Key.m_gui)
            {
                Expired.Add(entry.Key);
                continue;
            }

            entry.Key.m_position = entry.Value - Vector3.up * (GroundPingLabelAnchors.LabelOffset + dt * GroundPingLabelAnchors.RiseSpeed);
        }

        foreach (Chat.WorldTextInstance worldText in Expired) GroundPingLabelAnchors.Anchors.Remove(worldText);
        Expired.Clear();
    }

    private static void Postfix(List<Chat.WorldTextInstance> ___m_worldTexts)
    {
        OutlineManager.UpdatePings(___m_worldTexts);
        if (!Enabled.IsOn()) return;

        foreach (KeyValuePair<Chat.WorldTextInstance, Vector3> entry in GroundPingLabelAnchors.Anchors)
            entry.Key.m_position = entry.Value;
    }
}