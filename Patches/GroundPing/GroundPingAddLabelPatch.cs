using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using JoksterCube.PingMark.Domain.PingTargets;
using JoksterCube.PingMark.Domain.Models;
using JoksterCube.PingMark.Patches.PingLabels;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.GroundPing;

[HarmonyPatch(typeof(Chat), nameof(Chat.AddInworldText))]
internal static class GroundPingAddLabelPatch
{
    private static void Prefix(string text, out string __state) => __state = text;

    private static void Postfix(long senderID, Vector3 position, Talker.Type type, string __state, List<Chat.WorldTextInstance> ___m_worldTexts)
    {
        string text = __state;
        Chat.WorldTextInstance? worldText = ___m_worldTexts.Find(world => world.m_talkerID == senderID);
        if (worldText == null) return;

        OutlineManager.SetPingTarget(worldText, null);
        if (!Enabled.IsOn())
        {
            GroundPingLabelAnchors.Anchors.Remove(worldText);
            return;
        }

        if (type == Talker.Type.Ping) GroundPingLabelAnchors.Anchors[worldText] = position;
        else GroundPingLabelAnchors.Anchors.Remove(worldText);

        if (type == Talker.Type.Ping && ZNet.instance
            && senderID == ZNet.GetUID()
            && MobTargeting.TryConsumeGroundPing(position, out string? targetName, out PingTarget? target))
        {
            OutlineManager.SetPingTarget(worldText, target ?? PingTargetResolver.FindTarget(position));
            if (!string.IsNullOrWhiteSpace(targetName)) text = targetName!;
        }
        else
        {
            if (type == Talker.Type.Ping)
                OutlineManager.SetPingTarget(worldText, PingTargetResolver.FindTarget(position));
        }

        if (type != Talker.Type.Ping || string.IsNullOrWhiteSpace(text)) return;
        worldText.m_text = text;
        PingMarkLabels.Invalidate(worldText);
        Player localPlayer = Player.m_localPlayer;
        if (localPlayer && worldText.m_textMeshField)
            worldText.m_textMeshField.text = PingMarkLabels.Get(worldText, localPlayer);
    }
}