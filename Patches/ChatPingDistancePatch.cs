using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using JoksterCube.PingDistance.Common;
using TMPro;
using UnityEngine;
using static JoksterCube.PingDistance.Settings.PluginConfig;

namespace JoksterCube.PingDistance.Patches;

internal static class PingDistanceLabels
{
    private static readonly Dictionary<Chat.WorldTextInstance, string> Cache = new();
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

        // Clearing also drops instances of expired pings.
        Cache.Clear();
        foreach (Chat.WorldTextInstance worldText in worldTexts)
        {
            if (worldText.m_type == Talker.Type.Ping)
                worldText.m_textMeshField.text = Get(worldText, localPlayer);
        }
    }

    private static string Build(Chat.WorldTextInstance worldText, Player localPlayer)
    {
        return $"{worldText.m_userInfo.GetDisplayName()}\n{FormatDistance(worldText.m_position, localPlayer)}";
    }

    internal static string FormatDistance(Vector3 position, Player localPlayer)
    {
        float distanceMeters = Vector3.Distance(localPlayer.transform.position, position);
        return UseKilometers.Value == Toggle.On && distanceMeters >= 1000f
            ? $"{(distanceMeters / 1000f).ToString("F2", CultureInfo.InvariantCulture)} km"
            : $"{Mathf.RoundToInt(distanceMeters)} m";
    }
}

[HarmonyPatch(typeof(Chat), nameof(Chat.UpdateWorldTextField))]
internal static class PingWorldTextLabelPatch
{
    private static void Postfix(Chat.WorldTextInstance __0)
    {
        Chat.WorldTextInstance worldText = __0;
        if (worldText.m_type != Talker.Type.Ping || Enabled.Value != Toggle.On) return;

        Player localPlayer = Player.m_localPlayer;
        if (!localPlayer) return;

        PingDistanceLabels.Invalidate(worldText);
        worldText.m_textMeshField.text = PingDistanceLabels.Get(worldText, localPlayer);
    }
}

[HarmonyPatch(typeof(Chat), nameof(Chat.UpdateWorldTexts))]
internal static class PingWorldTextRefreshPatch
{
    private static readonly FieldInfo PinNamePrefabField = AccessTools.Field(typeof(Minimap), "m_pinNamePrefab");
    private static TMP_FontAsset? _mapFont;

    private static void Postfix(float dt, List<Chat.WorldTextInstance> ___m_worldTexts)
    {
        if (Enabled.Value != Toggle.On) return;

        if (!_mapFont && Minimap.instance && PinNamePrefabField.GetValue(Minimap.instance) is GameObject pinNamePrefab && pinNamePrefab)
        {
            PingDistanceLabels.MapLabelTemplate = pinNamePrefab.GetComponentInChildren<TMP_Text>(true);
            _mapFont = PingDistanceLabels.MapLabelTemplate?.font;
        }
        PingDistanceLabels.MapFont = _mapFont;

        foreach (Chat.WorldTextInstance worldText in ___m_worldTexts)
        {
            if (worldText.m_type != Talker.Type.Ping || !worldText.m_textMeshField) continue;

            TMP_Text label = worldText.m_textMeshField;
            if (_mapFont && label.font != _mapFont) label.font = _mapFont;
            if (label.color != PingColor.Value) label.color = PingColor.Value;
            PingDistanceLabels.MapLabelTemplate = label;
        }

        Player localPlayer = Player.m_localPlayer;
        if (!localPlayer) return;

        PingDistanceLabels.Tick(dt, ___m_worldTexts, localPlayer);
    }
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePingPins))]
internal static class PingMapLabelPatch
{
    private static readonly FieldInfo PinNameField = AccessTools.Field(typeof(Minimap.PinData), "m_name");

    private static void Postfix(List<Minimap.PinData> ___m_pingPins, List<Chat.WorldTextInstance> ___m_tempShouts)
    {
        if (Enabled.Value != Toggle.On) return;

        Player localPlayer = Player.m_localPlayer;
        if (!localPlayer) return;

        int count = Mathf.Min(___m_pingPins.Count, ___m_tempShouts.Count);
        for (int i = 0; i < count; i++)
        {
            Chat.WorldTextInstance worldText = ___m_tempShouts[i];
            if (worldText.m_type != Talker.Type.Ping) continue;

            Minimap.PinData pingPin = ___m_pingPins[i];
            string label = PingDistanceLabels.Get(worldText, localPlayer);
            PinNameField.SetValue(pingPin, label);

            if (pingPin.m_NamePinData != null && pingPin.m_NamePinData.PinNameText)
                pingPin.m_NamePinData.PinNameText.text = label;
        }
    }
}