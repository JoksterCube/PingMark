using System;
using System.Collections.Generic;
using System.Globalization;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using TMPro;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

internal static class PingLabelFormatter
{
    private static readonly Dictionary<Chat.WorldTextInstance, string> Cache = [];
    private static float _timer;

    internal static TMP_FontAsset? MapFont { get; set; }
    internal static TMP_Text? MapLabelTemplate { get; set; }

    // Unity-aware fallback: `??` would return a destroyed template instead of the fallback.
    internal static TMP_Text StyleSource(TMP_Text fallback) => MapLabelTemplate ? MapLabelTemplate! : fallback;

    internal static string Get(Chat.WorldTextInstance worldText, Player localPlayer)
    {
        if (!Cache.TryGetValue(worldText, out string label))
            Cache[worldText] = label = Build(worldText, localPlayer);
        return label;
    }

    internal static void Invalidate(Chat.WorldTextInstance worldText) => Cache.Remove(worldText);

    internal static void Clear() => Cache.Clear();

    internal static void Tick(float dt, List<Chat.WorldTextInstance> worldTexts, Player localPlayer)
    {
        _timer += dt;
        if (_timer < RefreshInterval.Value) return;
        _timer = 0f;

        Cache.Clear();
        foreach (Chat.WorldTextInstance worldText in worldTexts)
        {
            if (worldText.m_type == Talker.Type.Ping && worldText.m_textMeshField)
                worldText.m_textMeshField.text = Get(worldText, localPlayer);
        }
    }

    private static string Build(Chat.WorldTextInstance worldText, Player localPlayer)
    {
        List<string> lines = [];
        if (ShowPingerName.IsOn()) lines.Add(worldText.m_userInfo.GetDisplayName());

        string targetName = worldText.m_text.Trim();
        if (ShowTargetName.IsOn() && !string.IsNullOrWhiteSpace(targetName)
            && !string.Equals(targetName, "Ping", StringComparison.OrdinalIgnoreCase))
        {
            lines.Add(targetName);
        }

        if (ShowDistance.IsOn()) lines.Add(FormatDistance(worldText.m_position, localPlayer));
        if (lines.Count > 0) return string.Join("\n", lines);

        return string.IsNullOrWhiteSpace(EmptyLabelText.Value) ? "PING" : EmptyLabelText.Value.Trim();
    }

    internal static string FormatDistance(Vector3 position, Player localPlayer)
    {
        float distanceMeters = Vector3.Distance(localPlayer.transform.position, position);
        return UseKilometers.IsOn() && distanceMeters >= 1000f
            ? $"{(distanceMeters / 1000f).ToString("F2", CultureInfo.InvariantCulture)} km"
            : $"{Mathf.RoundToInt(distanceMeters)} m";
    }

    internal static string GetCharacterLabel(Character character, string distance)
    {
        if (ShowPingerName.IsOn())
        {
            string name = MobHighlight.GetPingerName(character);
            return ShowDistance.IsOn() ? name + "\n" + distance : name;
        }
        return ShowDistance.IsOn() ? distance : string.Empty;
    }

    // " (Hungry)" / " (Happy)" for tamed creatures, " (15%)" while taming; empty for wild or untameable ones.
    internal static string GetTameStatus(Character character)
    {
        if (!character.TryGetComponent(out Tameable tameable)) return string.Empty;
        if (tameable.IsTamed())
        {
            string status = Localization.instance?.Localize(tameable.GetStatusString()) ?? string.Empty;
            return string.IsNullOrWhiteSpace(status) || status.StartsWith("$") ? string.Empty : $" ({status.Trim()})";
        }
        int tameness = tameable.GetTameness();
        return tameness > 0 ? $" ({tameness}%)" : string.Empty;
    }
}
