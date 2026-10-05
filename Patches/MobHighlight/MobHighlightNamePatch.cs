using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using JoksterCube.PingMark.Domain.PingTargets;
using JoksterCube.PingMark.Patches.PingLabels;
using MobHighlightService = JoksterCube.PingMark.Domain.MobHighlight;
using TMPro;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.MobHighlight;

[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.LateUpdate))]
internal static class MobHighlightNamePatch
{
    private static readonly Dictionary<TMP_Text, string> OriginalNames = [];
    private static readonly Dictionary<TMP_Text, TMP_Text> PlayerLabels = [];
    private static readonly Dictionary<TMP_Text, List<(Transform Transform, Vector3 Position)>> OriginalMarkers = [];
    private static readonly Dictionary<TMP_Text, string> Distances = [];
    private static readonly Dictionary<TMP_Text, (ZDOID Id, long Order, string Distance, bool ShowName, bool ShowDistance, string Text)> Labels = [];
    private static readonly Dictionary<TMP_Text, (Bounds Bounds, Rect Rect)> NameLayouts = [];
    private static readonly Dictionary<TMP_Text, (TMP_Text Label, TMP_FontAsset? Font, float Size, bool Auto, float Min, float Max, FontStyles Style, float Line, float Character, float Word, float Paragraph, float Width)> LabelStyles = [];
    private static readonly List<TMP_Text> Expired = [];
    private static float _distanceTimer;

    private static void Prefix(Dictionary<Character, EnemyHud.HudData> ___m_huds)
    {
        foreach (KeyValuePair<Character, EnemyHud.HudData> entry in ___m_huds)
            if (MobHighlightService.IsMarked(entry.Key)) entry.Value.m_hoverTimer = 0f;
    }

    private static void Postfix(Dictionary<Character, EnemyHud.HudData> ___m_huds)
    {
        _distanceTimer += Time.deltaTime;
        if (_distanceTimer >= RefreshInterval.Value)
        {
            _distanceTimer = 0f;
            Distances.Clear();
        }

        Player localPlayer = Player.m_localPlayer;
        foreach (KeyValuePair<Character, EnemyHud.HudData> entry in ___m_huds)
        {
            TMP_Text name = entry.Value.m_name;
            if (!name) continue;

            if (!MobHighlightService.IsMarked(entry.Key))
            {
                if (OriginalNames.TryGetValue(name, out string originalName))
                {
                    name.text = originalName;
                    OriginalNames.Remove(name);
                }
                if (PlayerLabels.TryGetValue(name, out TMP_Text playerLabel))
                {
                    if (playerLabel) Object.Destroy(playerLabel.gameObject);
                    PlayerLabels.Remove(name);
                }
                if (OriginalMarkers.TryGetValue(name, out List<(Transform Transform, Vector3 Position)> markers))
                {
                    foreach ((Transform transform, Vector3 position) in markers)
                        if (transform) transform.localPosition = position;
                    OriginalMarkers.Remove(name);
                }
                Distances.Remove(name);
                Labels.Remove(name);
                NameLayouts.Remove(name);
                LabelStyles.Remove(name);
                continue;
            }

            if (!OriginalNames.TryGetValue(name, out string original)) OriginalNames[name] = original = name.text;
            if (localPlayer)
            {
                if (!Distances.TryGetValue(name, out string distance))
                    Distances[name] = distance = PingMarkLabels.FormatDistance(entry.Key.transform.position, localPlayer);
                if (name.text != original) name.text = original;

                if (PingMarkLabels.MapFont
                    && (!PlayerLabels.TryGetValue(name, out TMP_Text existingPlayerLabel) || !existingPlayerLabel))
                {
                    RectTransform rect = name.rectTransform;
                    GameObject playerLabelObject = Object.Instantiate(name.gameObject, name.transform.parent, false);
                    playerLabelObject.name = $"{name.gameObject.name} PingMark";
                    TMP_Text playerLabel = playerLabelObject.GetComponent<TMP_Text>();
                    RectTransform playerRect = playerLabel.rectTransform;
                    TMP_Text styleSource = PingMarkLabels.MapLabelTemplate ?? name;
                    float lineHeight = styleSource.GetPreferredValues("Mg").y;
                    List<(Transform Transform, Vector3 Position)> markers = [];
                    Transform?[] markerTransforms = [entry.Value.m_alerted?.transform, entry.Value.m_aware?.transform];
                    foreach (Transform? marker in markerTransforms)
                    {
                        if (!marker) continue;
                        markers.Add((marker, marker.localPosition));
                        marker.localPosition += Vector3.up * (lineHeight * 2f);
                    }
                    OriginalMarkers[name] = markers;
                    playerRect.SetParent(rect, false);
                    playerRect.anchorMin = rect.pivot;
                    playerRect.anchorMax = rect.pivot;
                    playerRect.pivot = new Vector2(0.5f, 0f);
                    playerRect.sizeDelta = new Vector2(rect.rect.width, lineHeight * 2f);
                    playerRect.localScale = Vector3.one;
                    for (int i = 0; i < playerLabelObject.transform.childCount; i++)
                        playerLabelObject.transform.GetChild(i).gameObject.SetActive(false);
                    playerLabel.font = PingMarkLabels.MapFont;
                    playerLabel.fontSize = styleSource.fontSize;
                    playerLabel.enableAutoSizing = styleSource.enableAutoSizing;
                    playerLabel.fontSizeMin = styleSource.fontSizeMin;
                    playerLabel.fontSizeMax = styleSource.fontSizeMax;
                    playerLabel.fontStyle = styleSource.fontStyle;
                    playerLabel.alignment = TextAlignmentOptions.Bottom;
                    playerLabel.lineSpacing = styleSource.lineSpacing;
                    playerLabel.characterSpacing = styleSource.characterSpacing;
                    playerLabel.wordSpacing = styleSource.wordSpacing;
                    playerLabel.paragraphSpacing = styleSource.paragraphSpacing;
                    playerLabel.color = Color.white;
                    playerLabel.raycastTarget = false;
                    PlayerLabels[name] = playerLabel;
                }

                if (PlayerLabels.TryGetValue(name, out TMP_Text activePlayerLabel) && activePlayerLabel)
                {
                    ZDOID id = entry.Key.GetZDOID();
                    long order = MobHighlightService.GetPingerOrder(entry.Key);
                    bool showName = ShowPingerName.IsOn();
                    bool showDistance = ShowDistance.IsOn();
                    if (!Labels.TryGetValue(name, out var label) || !label.Id.Equals(id)
                        || label.Order != order || label.Distance != distance
                        || label.ShowName != showName || label.ShowDistance != showDistance)
                    {
                        string text = PingMarkLabels.GetCharacterLabel(entry.Key, distance);
                        Labels[name] = label = (id, order, distance, showName, showDistance, text);
                    }
                    if (activePlayerLabel.text != label.Text) activePlayerLabel.text = label.Text;
                    bool visible = label.Text.Length > 0;
                    if (activePlayerLabel.gameObject.activeSelf != visible) activePlayerLabel.gameObject.SetActive(visible);
                    UpdateLabelStyle(name, activePlayerLabel);
                    Rect rect = name.rectTransform.rect;
                    if (!NameLayouts.TryGetValue(name, out var layout) || name.havePropertiesChanged || layout.Rect != rect)
                    {
                        name.ForceMeshUpdate();
                        NameLayouts[name] = layout = (name.textBounds, rect);
                    }
                    Vector2 position = new(layout.Bounds.center.x, layout.Bounds.max.y + 2f);
                    if (activePlayerLabel.rectTransform.anchoredPosition != position)
                        activePlayerLabel.rectTransform.anchoredPosition = position;
                }
            }
        }

        foreach (TMP_Text name in OriginalNames.Keys)
            if (!name) Expired.Add(name!);
        foreach (TMP_Text name in Expired)
        {
            if (PlayerLabels.TryGetValue(name, out TMP_Text playerLabel))
            {
                if (playerLabel) Object.Destroy(playerLabel.gameObject);
            }
            OriginalNames.Remove(name);
            PlayerLabels.Remove(name);
            OriginalMarkers.Remove(name);
            Distances.Remove(name);
            Labels.Remove(name);
            NameLayouts.Remove(name);
            LabelStyles.Remove(name);
        }
        Expired.Clear();
    }

    private static void UpdateLabelStyle(TMP_Text name, TMP_Text label)
    {
        TMP_Text source = PingMarkLabels.MapLabelTemplate ?? name;
        var style = (label, PingMarkLabels.MapFont, source.fontSize, source.enableAutoSizing,
            source.fontSizeMin, source.fontSizeMax, source.fontStyle, source.lineSpacing,
            source.characterSpacing, source.wordSpacing, source.paragraphSpacing, name.rectTransform.rect.width);
        if (LabelStyles.TryGetValue(name, out var cached) && cached.Equals(style)) return;
        LabelStyles[name] = style;

        label.font = PingMarkLabels.MapFont;
        label.fontSize = source.fontSize;
        label.enableAutoSizing = source.enableAutoSizing;
        label.fontSizeMin = source.fontSizeMin;
        label.fontSizeMax = source.fontSizeMax;
        label.fontStyle = source.fontStyle;
        label.lineSpacing = source.lineSpacing;
        label.characterSpacing = source.characterSpacing;
        label.wordSpacing = source.wordSpacing;
        label.paragraphSpacing = source.paragraphSpacing;
        float lineHeight = source.GetPreferredValues("Mg").y;
        label.rectTransform.sizeDelta = new Vector2(name.rectTransform.rect.width, lineHeight * 2f);
        if (OriginalMarkers.TryGetValue(name, out var markers))
            foreach ((Transform transform, Vector3 position) in markers)
                if (transform) transform.localPosition = position + Vector3.up * (lineHeight * 2f);
    }
}