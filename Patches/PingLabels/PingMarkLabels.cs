using System;
using System.Collections.Generic;
using System.Globalization;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using MobHighlightService = JoksterCube.PingMark.Domain.MobHighlight;
using TMPro;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.PingLabels;

internal static class PingMarkLabels
{
    private static readonly Dictionary<Chat.WorldTextInstance, string> Cache = [];
    private static float _timer;

    internal static TMP_FontAsset? MapFont { get; set; }
    internal static TMP_Text? MapLabelTemplate { get; set; }

    internal static string Get(Chat.WorldTextInstance worldText, Player localPlayer)
    {
        if (!Cache.TryGetValue(worldText, out string label))
            Cache[worldText] = label = Build(worldText, localPlayer);
        return label;
    }

    internal static void Invalidate(Chat.WorldTextInstance worldText) => Cache.Remove(worldText);

    internal static void Tick(float dt, List<Chat.WorldTextInstance> worldTexts, Player localPlayer)
    {
        _timer += dt;
        if (_timer < RefreshInterval.Value) return;
        _timer = 0f;

        Cache.Clear();
        foreach (Chat.WorldTextInstance worldText in worldTexts)
        {
            if (worldText.m_type == Talker.Type.Ping)
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
            string name = MobHighlightService.GetPingerName(character);
            return ShowDistance.IsOn() ? name + "\n" + distance : name;
        }
        return ShowDistance.IsOn() ? distance : string.Empty;
    }
}