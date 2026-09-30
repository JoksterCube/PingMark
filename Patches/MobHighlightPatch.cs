using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.PingDistance.Domain;
using TMPro;
using UnityEngine;
using static JoksterCube.PingDistance.Settings.PluginConfig;

namespace JoksterCube.PingDistance.Patches;

[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.TestShow))]
internal static class MobHighlightVisibilityPatch
{
    private static void Postfix(Character __0, ref bool __result)
    {
        if (MobHighlight.IsMarked(__0)) __result = true;
    }
}

[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.LateUpdate))]
internal static class MobHighlightNamePatch
{
    private static readonly Dictionary<TMP_Text, string> OriginalNames = [];
    private static readonly Dictionary<TMP_Text, TMP_Text> PlayerLabels = [];
    private static readonly Dictionary<TMP_Text, List<(Transform Transform, Vector3 Position)>> OriginalMarkers = [];
    private static readonly Dictionary<TMP_Text, string> Distances = [];
    private static readonly List<TMP_Text> Expired = [];
    private static float _distanceTimer;

    private static void Prefix(Dictionary<Character, EnemyHud.HudData> ___m_huds)
    {
        foreach (KeyValuePair<Character, EnemyHud.HudData> entry in ___m_huds)
            if (MobHighlight.IsMarked(entry.Key)) entry.Value.m_hoverTimer = 0f;
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

            if (!MobHighlight.IsMarked(entry.Key))
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
                continue;
            }

            if (!OriginalNames.ContainsKey(name)) OriginalNames[name] = name.text;
            if (localPlayer)
            {
                if (!Distances.TryGetValue(name, out string distance))
                    Distances[name] = distance = PingDistanceLabels.FormatDistance(entry.Key.transform.position, localPlayer);
                name.text = OriginalNames[name];

                if (PingDistanceLabels.MapFont
                    && (!PlayerLabels.TryGetValue(name, out TMP_Text existingPlayerLabel) || !existingPlayerLabel))
                {
                    RectTransform rect = name.rectTransform;
                    GameObject playerLabelObject = Object.Instantiate(name.gameObject, name.transform.parent, false);
                    playerLabelObject.name = $"{name.gameObject.name} PingDistance";
                    TMP_Text playerLabel = playerLabelObject.GetComponent<TMP_Text>();
                    RectTransform playerRect = playerLabel.rectTransform;
                    TMP_Text styleSource = PingDistanceLabels.MapLabelTemplate ?? name;
                    float lineHeight = styleSource.GetPreferredValues("Mg").y;
                    List<(Transform Transform, Vector3 Position)> markers = [];
                    for (int i = 0; i < name.transform.childCount; i++)
                    {
                        Transform marker = name.transform.GetChild(i);
                        markers.Add((marker, marker.localPosition));
                        marker.localPosition += Vector3.up * (lineHeight * 2f);
                    }
                    OriginalMarkers[name] = markers;
                    playerRect.anchorMin = rect.anchorMin;
                    playerRect.anchorMax = rect.anchorMax;
                    playerRect.pivot = rect.pivot;
                    playerRect.sizeDelta = new Vector2(rect.sizeDelta.x, lineHeight * 2f);
                    playerRect.anchoredPosition = rect.anchoredPosition + Vector2.up * (lineHeight * 1.5f);
                    playerRect.localScale = rect.localScale;
                    for (int i = 0; i < playerLabelObject.transform.childCount; i++)
                        playerLabelObject.transform.GetChild(i).gameObject.SetActive(false);
                    playerLabel.font = PingDistanceLabels.MapFont;
                    playerLabel.fontSize = styleSource.fontSize;
                    playerLabel.enableAutoSizing = styleSource.enableAutoSizing;
                    playerLabel.fontSizeMin = styleSource.fontSizeMin;
                    playerLabel.fontSizeMax = styleSource.fontSizeMax;
                    playerLabel.fontStyle = styleSource.fontStyle;
                    playerLabel.alignment = styleSource.alignment;
                    playerLabel.lineSpacing = styleSource.lineSpacing;
                    playerLabel.characterSpacing = styleSource.characterSpacing;
                    playerLabel.wordSpacing = styleSource.wordSpacing;
                    playerLabel.paragraphSpacing = styleSource.paragraphSpacing;
                    playerLabel.color = Color.white;
                    playerLabel.raycastTarget = false;
                    PlayerLabels[name] = playerLabel;
                }

                if (PlayerLabels.TryGetValue(name, out TMP_Text activePlayerLabel) && activePlayerLabel)
                    activePlayerLabel.text = $"{UserInfo.GetLocalUser().GetDisplayName()}\n{distance}";
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
        }
        Expired.Clear();
    }
}