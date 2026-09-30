using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.PingDistance.Common;
using JoksterCube.PingDistance.Domain;
using static JoksterCube.PingDistance.Settings.PluginConfig;
using UnityEngine;

namespace JoksterCube.PingDistance.Patches;

internal static class GroundPingLabelAnchors
{
    internal const float LabelOffset = 0.3f;
    internal const float RiseSpeed = 0.15f;
    internal static readonly Dictionary<Chat.WorldTextInstance, Vector3> Anchors = new();
}

[HarmonyPatch(typeof(Chat), nameof(Chat.AddInworldText))]
internal static class GroundPingAddLabelPatch
{
    private static void Postfix(long senderID, Vector3 position, Talker.Type type, List<Chat.WorldTextInstance> ___m_worldTexts)
    {
        Chat.WorldTextInstance? worldText = ___m_worldTexts.Find(text => text.m_talkerID == senderID);
        if (worldText == null) return;

        if (Enabled.Value != Toggle.On)
        {
            GroundPingLabelAnchors.Anchors.Remove(worldText);
            return;
        }

        if (type == Talker.Type.Ping && MobTargeting.TryConsumeGroundPing(position))
            GroundPingLabelAnchors.Anchors[worldText] = position;
        else
            GroundPingLabelAnchors.Anchors.Remove(worldText);
    }
}

[HarmonyPatch(typeof(Chat), nameof(Chat.UpdateWorldTexts))]
internal static class GroundPingUpdateLabelPatch
{
    private static readonly List<Chat.WorldTextInstance> Expired = new();

    private static void Prefix(float dt)
    {
        if (Enabled.Value != Toggle.On)
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

    private static void Postfix()
    {
        if (Enabled.Value != Toggle.On) return;

        foreach (KeyValuePair<Chat.WorldTextInstance, Vector3> entry in GroundPingLabelAnchors.Anchors)
            entry.Key.m_position = entry.Value;
    }
}