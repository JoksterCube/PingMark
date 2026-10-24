using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static JoksterCube.PingMark.Domain.Collections.PrefabCollectionEditorUI;

namespace JoksterCube.PingMark.Domain.Collections;

// IMGUI styles borrowed from the game's menu, plus the button and label widgets that use them.
internal static class PrefabEditorStyles
{
    internal static GUIStyle? TextStyle { get; private set; }
    internal static GUIStyle? TitleStyle { get; private set; }
    internal static GUIStyle? CountStyle { get; private set; }
    internal static GUIStyle? RowStyle { get; private set; }
    internal static GUIStyle? ButtonStyle { get; private set; }
    internal static GUIStyle? SearchStyle { get; private set; }
    internal static Sprite? PanelSprite { get; private set; }
    internal static Sprite? ButtonSprite { get; private set; }

    internal static void Reset()
    {
        TextStyle = null;
        RowStyle = null;
    }

    // Returns true when the styles were (re)created, so cached text measurements must be discarded.
    internal static bool Ensure()
    {
        if (TextStyle != null) return false;
        TMP_Text? menuText = Menu.instance && Menu.instance.m_continueButton
            ? Menu.instance.m_continueButton.GetComponentInChildren<TMP_Text>(true) : null;
        Font? font = menuText && menuText!.font ? menuText.font.sourceFontFile : null;
        TextStyle = new GUIStyle(GUI.skin.label) { font = font ? font : GUI.skin.font, fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip };
        TextStyle.normal.textColor = new Color(0.92f, 0.87f, 0.72f);
        TitleStyle = new GUIStyle(TextStyle) { fontSize = 22 };
        CountStyle = new GUIStyle(TextStyle) { fontSize = 12 };
        CountStyle.normal.textColor = new Color(0.72f, 0.72f, 0.65f);
        RowStyle = new GUIStyle(TextStyle) { fontSize = 14, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset(6, 6, 0, 0) };
        RowStyle.hover.textColor = Color.white;
        ButtonStyle = new GUIStyle(GUI.skin.button) { font = TextStyle.font, fontSize = 14, wordWrap = false, clipping = TextClipping.Clip };
        ButtonStyle.normal.textColor = TextStyle.normal.textColor;
        SearchStyle = new GUIStyle(GUI.skin.textField) { font = TextStyle.font, fontSize = 16, wordWrap = false, alignment = TextAnchor.MiddleLeft };
        PanelSprite = null;
        ButtonSprite = Menu.instance && Menu.instance.m_continueButton && Menu.instance.m_continueButton.image
            ? Menu.instance.m_continueButton.image.sprite : null;
        if (ButtonSprite)
            foreach (GUIStyleState state in (GUIStyleState[])[ButtonStyle.normal, ButtonStyle.hover, ButtonStyle.active, ButtonStyle.focused])
                state.background = null;
        if (InventoryGui.instance)
            PanelSprite = InventoryGui.instance.m_inventoryRoot.GetComponentsInChildren<Image>(true)
                .Where(image => image.sprite && (image.name.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0
                    || image.name.IndexOf("bkg", StringComparison.OrdinalIgnoreCase) >= 0
                    || image.sprite.name.IndexOf("wood", StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(image => image.sprite).FirstOrDefault();
        return true;
    }

    internal static bool Button(Rect rect, GUIContent content, bool selected = false)
    {
        if (ButtonSprite)
        {
            Color previous = GUI.color;
            GUI.color = !GUI.enabled ? new Color(0.5f, 0.5f, 0.5f) : rect.Contains(Event.current.mousePosition)
                ? new Color(1f, 0.9f, 0.7f) : Color.white;
            DrawSprite(rect, ButtonSprite!);
            GUI.color = previous;
        }
        if (selected) Fill(rect, new Color(0.7f, 0.55f, 0.18f, 0.4f));
        int originalSize = ButtonStyle!.fontSize;
        try
        {
            FitFont(ButtonStyle, content, rect);
            return GUI.Button(rect, content, ButtonStyle);
        }
        finally { ButtonStyle.fontSize = originalSize; }
    }

    internal static void Label(Rect rect, string text, GUIStyle? style)
    {
        if (style == null) return;
        int originalSize = style.fontSize;
        try
        {
            GUIContent content = new(text);
            FitFont(style, content, rect);
            GUI.Label(rect, content, style);
        }
        finally { style.fontSize = originalSize; }
    }
}
