using System;
using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.Highlights;

[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.LateUpdate))]
internal static class MobHighlightNamePatch
{
    private const string Feature = "Creature nameplates";

    private static readonly Dictionary<TMP_Text, Nameplate> Nameplates = [];
    private static readonly List<TMP_Text> Expired = [];
    private static float _distanceTimer;

    // Everything PingMark adds to one vanilla nameplate, so it can be restored in one place.
    private sealed class Nameplate(string originalName)
    {
        internal readonly string OriginalName = originalName;
        internal readonly List<(Transform Transform, Vector3 Position)> Markers = [];
        internal TMP_Text? Label;
        internal string? Distance;
        internal string? Status;
        internal (ZDOID Id, long Order, string Distance, bool ShowName, bool ShowDistance)? TextKey;
        internal string Text = string.Empty;
        internal (Bounds Bounds, Rect Rect)? Layout;
        internal (TMP_FontAsset? Font, float Size, bool Auto, float Min, float Max, FontStyles Style,
            float Line, float Character, float Word, float Paragraph, float Width)? Style;

        internal void Restore(TMP_Text? name)
        {
            if (name) name!.text = OriginalName;
            if (Label) Object.Destroy(Label!.gameObject);
            foreach ((Transform transform, Vector3 position) in Markers)
                if (transform) transform.localPosition = position;
        }
    }

    private static void Prefix(Dictionary<Character, EnemyHud.HudData> ___m_huds)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { PrefixCore(___m_huds); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void PrefixCore(Dictionary<Character, EnemyHud.HudData> ___m_huds)
    {
        foreach (KeyValuePair<Character, EnemyHud.HudData> entry in ___m_huds)
            if (MobHighlight.IsMarked(entry.Key)) entry.Value.m_hoverTimer = 0f;
    }

    private static void Postfix(Dictionary<Character, EnemyHud.HudData> ___m_huds)
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { PostfixCore(___m_huds); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void PostfixCore(Dictionary<Character, EnemyHud.HudData> ___m_huds)
    {
        _distanceTimer += Time.deltaTime;
        if (_distanceTimer >= RefreshInterval.Value)
        {
            _distanceTimer = 0f;
            foreach (Nameplate nameplate in Nameplates.Values) nameplate.Distance = nameplate.Status = null;
        }

        Player localPlayer = Player.m_localPlayer;
        foreach (KeyValuePair<Character, EnemyHud.HudData> entry in ___m_huds)
        {
            TMP_Text name = entry.Value.m_name;
            if (!name) continue;

            if (!MobHighlight.IsMarked(entry.Key))
            {
                if (Nameplates.TryGetValue(name, out Nameplate restored))
                {
                    restored.Restore(name);
                    Nameplates.Remove(name);
                }
                continue;
            }

            if (!Nameplates.TryGetValue(name, out Nameplate nameplate))
                Nameplates[name] = nameplate = new Nameplate(name.text);
            if (localPlayer) UpdateNameplate(entry.Key, entry.Value, name, nameplate, localPlayer);
        }

        foreach (TMP_Text name in Nameplates.Keys)
            if (!name) Expired.Add(name!);
        foreach (TMP_Text name in Expired)
        {
            Nameplates[name].Restore(null);
            Nameplates.Remove(name);
        }
        Expired.Clear();
    }

    private static void UpdateNameplate(Character character, EnemyHud.HudData hud, TMP_Text name, Nameplate nameplate, Player localPlayer)
    {
        nameplate.Distance ??= PingLabelFormatter.FormatDistance(character.transform.position, localPlayer);
        nameplate.Status ??= PingLabelFormatter.GetTameStatus(character);
        string shownName = nameplate.OriginalName + nameplate.Status;
        if (name.text != shownName) name.text = shownName;

        if (PingLabelFormatter.MapFont && !nameplate.Label) CreateLabel(hud, name, nameplate);
        TMP_Text? label = nameplate.Label;
        if (!label) return;

        ZDOID id = character.GetZDOID();
        long order = MobHighlight.GetPingerOrder(character);
        var key = (id, order, nameplate.Distance, ShowPingerName.IsOn(), ShowDistance.IsOn());
        if (!nameplate.TextKey.Equals(key))
        {
            nameplate.TextKey = key;
            nameplate.Text = PingLabelFormatter.GetCharacterLabel(character, nameplate.Distance);
        }
        if (label!.text != nameplate.Text) label.text = nameplate.Text;
        bool visible = nameplate.Text.Length > 0;
        if (label.gameObject.activeSelf != visible) label.gameObject.SetActive(visible);
        UpdateLabelStyle(name, label, nameplate);

        Rect rect = name.rectTransform.rect;
        if (nameplate.Layout is not { } layout || name.havePropertiesChanged || layout.Rect != rect)
        {
            name.ForceMeshUpdate();
            nameplate.Layout = layout = (name.textBounds, rect);
        }
        Vector2 position = new(layout.Bounds.center.x, layout.Bounds.max.y + 2f);
        if (label.rectTransform.anchoredPosition != position) label.rectTransform.anchoredPosition = position;
    }

    private static void CreateLabel(EnemyHud.HudData hud, TMP_Text name, Nameplate nameplate)
    {
        RectTransform rect = name.rectTransform;
        GameObject labelObject = Object.Instantiate(name.gameObject, name.transform.parent, false);
        labelObject.name = $"{name.gameObject.name} PingMark";
        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        RectTransform labelRect = label.rectTransform;

        // Markers are recorded once; a recreated label must not shift them a second time.
        if (nameplate.Markers.Count == 0)
            foreach (Transform? marker in (Transform?[])[hud.m_alerted, hud.m_aware])
                if (marker) nameplate.Markers.Add((marker!, marker!.localPosition));

        labelRect.SetParent(rect, false);
        labelRect.anchorMin = rect.pivot;
        labelRect.anchorMax = rect.pivot;
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.localScale = Vector3.one;
        for (int i = 0; i < labelObject.transform.childCount; i++)
            labelObject.transform.GetChild(i).gameObject.SetActive(false);
        label.alignment = TextAlignmentOptions.Bottom;
        label.color = Color.white;
        label.raycastTarget = false;

        nameplate.Label = label;
        nameplate.Style = null;
        UpdateLabelStyle(name, label, nameplate);
    }

    private static void UpdateLabelStyle(TMP_Text name, TMP_Text label, Nameplate nameplate)
    {
        TMP_Text source = PingLabelFormatter.StyleSource(name);
        var style = (PingLabelFormatter.MapFont, source.fontSize, source.enableAutoSizing,
            source.fontSizeMin, source.fontSizeMax, source.fontStyle, source.lineSpacing,
            source.characterSpacing, source.wordSpacing, source.paragraphSpacing, name.rectTransform.rect.width);
        if (nameplate.Style.Equals(style)) return;
        nameplate.Style = style;

        label.font = PingLabelFormatter.MapFont;
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
        foreach ((Transform transform, Vector3 position) in nameplate.Markers)
            if (transform) transform.localPosition = position + Vector3.up * (lineHeight * 2f);
    }
}
