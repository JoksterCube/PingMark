using System;
using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using TMPro;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.PingLabels;

// Single patch for Chat.UpdateWorldTexts so anchoring always runs before labels measure their distance.
[HarmonyPatch(typeof(Chat), nameof(Chat.UpdateWorldTexts))]
internal static class WorldTextsUpdatePatch
{
    private const string Feature = "Ping labels";

    private static readonly List<Chat.WorldTextInstance> Expired = [];
    private static TMP_FontAsset? _mapFont;

    private static void Prefix(float dt, List<Chat.WorldTextInstance> ___m_worldTexts, float ___m_worldTextTTL)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { PrefixCore(dt, ___m_worldTexts, ___m_worldTextTTL); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void PrefixCore(float dt, List<Chat.WorldTextInstance> worldTexts, float ttl)
    {
        if (!Enabled.IsOn())
        {
            GroundPingLabelAnchors.Anchors.Clear();
            return;
        }

        // Vanilla removes every world text (pings and shouts alike) after the same few seconds. Pings run their timer
        // slower or faster instead, so they last Ping Duration and shouts keep the vanilla time.
        float extra = dt * (ttl / Mathf.Max(PingDuration.Value, 0.1f) - 1f);
        if (!Mathf.Approximately(extra, 0f))
            foreach (Chat.WorldTextInstance worldText in worldTexts)
                if (worldText.m_type == Talker.Type.Ping) worldText.m_timer += extra;

        // Pre-compensate vanilla's upward drift so the label stays below its anchor this frame.
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

    private static void Postfix(float dt, List<Chat.WorldTextInstance> ___m_worldTexts)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { PostfixCore(dt, ___m_worldTexts); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void PostfixCore(float dt, List<Chat.WorldTextInstance> ___m_worldTexts)
    {
        OutlineManager.UpdatePings(___m_worldTexts);
        if (!Enabled.IsOn()) return;

        foreach (KeyValuePair<Chat.WorldTextInstance, Vector3> entry in GroundPingLabelAnchors.Anchors)
            entry.Key.m_position = entry.Value;

        UpdateLabelStyles(___m_worldTexts);

        Player localPlayer = Player.m_localPlayer;
        if (localPlayer) PingLabelFormatter.Tick(dt, ___m_worldTexts, localPlayer);
    }

    private static void UpdateLabelStyles(List<Chat.WorldTextInstance> worldTexts)
    {
        if (!_mapFont && Minimap.instance && Minimap.instance.m_pinNamePrefab is { } pinNamePrefab && pinNamePrefab)
        {
            TMP_Text template = pinNamePrefab.GetComponentInChildren<TMP_Text>(true);
            PingLabelFormatter.MapLabelTemplate = template;
            _mapFont = template ? template.font : null;
        }
        PingLabelFormatter.MapFont = _mapFont;

        foreach (Chat.WorldTextInstance worldText in worldTexts)
        {
            if (worldText.m_type != Talker.Type.Ping || !worldText.m_textMeshField) continue;

            TMP_Text label = worldText.m_textMeshField;
            if (_mapFont && label.font != _mapFont) label.font = _mapFont;
            Color color = MatchTextToOutline.IsOn() && OutlineManager.TryGetPingColor(worldText, out Color outlineColor)
                ? outlineColor
                : PingColor.Value;
            if (label.color != color) label.color = color;
        }
    }
}
