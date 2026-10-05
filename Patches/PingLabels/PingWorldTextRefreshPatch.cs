using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using TMPro;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.PingLabels;

[HarmonyPatch(typeof(Chat), nameof(Chat.UpdateWorldTexts))]
internal static class PingWorldTextRefreshPatch
{
    private static readonly FieldInfo PinNamePrefabField = AccessTools.Field(typeof(Minimap), "m_pinNamePrefab");
    private static TMP_FontAsset? _mapFont;

    private static void Postfix(float dt, List<Chat.WorldTextInstance> ___m_worldTexts)
    {
        if (!Enabled.IsOn()) return;

        if (!_mapFont && Minimap.instance && PinNamePrefabField.GetValue(Minimap.instance) is GameObject pinNamePrefab && pinNamePrefab)
        {
            PingMarkLabels.MapLabelTemplate = pinNamePrefab.GetComponentInChildren<TMP_Text>(true);
            _mapFont = PingMarkLabels.MapLabelTemplate?.font;
        }
        PingMarkLabels.MapFont = _mapFont;

        foreach (Chat.WorldTextInstance worldText in ___m_worldTexts)
        {
            if (worldText.m_type != Talker.Type.Ping || !worldText.m_textMeshField) continue;

            TMP_Text label = worldText.m_textMeshField;
            if (_mapFont && label.font != _mapFont) label.font = _mapFont;
            Color color = MatchTextToOutline.IsOn() && OutlineManager.TryGetPingColor(worldText, out Color outlineColor)
                ? outlineColor
                : PingColor.Value;
            if (label.color != color) label.color = color;
            PingMarkLabels.MapLabelTemplate = label;
        }

        Player localPlayer = Player.m_localPlayer;
        if (!localPlayer) return;

        PingMarkLabels.Tick(dt, ___m_worldTexts, localPlayer);
    }
}