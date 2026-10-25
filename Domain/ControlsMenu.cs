using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

// Ping and Perception rows in the game's Controls tab. The game binds a single key, so a row sets the main key of
// its config shortcut and keeps the modifiers (Left Shift stays on Perception). Like the vanilla rows, a new key is
// only kept when the settings are confirmed.
internal static class ControlsMenu
{
    private static readonly (string Name, string Label, Func<ConfigEntry<KeyboardShortcut>> Entry)[] Rows =
    [
        ("PingMark_Ping", "Ping", () => PingShortcut),
        ("PingMark_Perception", "Perception", () => PerceptionShortcut)
    ];

    private static Dictionary<string, KeyCode>? _keyCodes;

    // Before the tab sets up its rows, so the new ones get the same click handling and gamepad navigation.
    internal static void AddRows(KeyboardMouseSettings settings)
    {
        ZInput input = ZInput.instance;
        if (input == null) return;
        // The menu's binding starts from the config every time, so a key chosen and then backed out of is undone.
        foreach (var row in Rows) Button(input, row.Name).Rebind(PathFor(row.Entry().Value.MainKey));

        List<KeySetting> keys = settings.m_keys;
        if (keys.Count == 0 || keys.Exists(key => key.m_keyName == Rows[0].Name)) return;
        // At the bottom of the first column, so neither column's vanilla rows move to the other.
        int insertAt = Mathf.Clamp(settings.m_keyRows, 1, keys.Count);
        KeySetting template = keys[insertAt - 1];
        int sibling = template.m_keyTransform.GetSiblingIndex() + 1;
        for (int index = 0; index < Rows.Length; index++)
        {
            RectTransform row = UnityEngine.Object.Instantiate(template.m_keyTransform, template.m_keyTransform.parent);
            row.name = Rows[index].Name;
            row.SetSiblingIndex(sibling + index);
            Transform label = row.Find("Label");
            if (label && label.TryGetComponent(out TMP_Text text)) text.text = Rows[index].Label;
            keys.Insert(insertAt + index, new KeySetting
            {
                m_keyName = Rows[index].Name,
                m_keyTransform = row,
                m_blockedButtons = template.m_blockedButtons ?? []
            });
        }
        int columns = Mathf.Max(1, settings.m_keyCols);
        settings.m_keyRows = Mathf.Max(settings.m_keyRows + Rows.Length, Mathf.CeilToInt(keys.Count / (float)columns));
        FitGrid(template.m_keyTransform.parent, settings.m_keyRows);
    }

    // The game shows the main key; the modifiers kept from the config go in front of it.
    internal static void UpdateLabels(KeyboardMouseSettings settings)
    {
        foreach (KeySetting key in settings.m_keys)
        {
            int index = Array.FindIndex(Rows, row => row.Name == key.m_keyName);
            if (index < 0) continue;
            TMP_Text text = key.m_keyTransform.GetComponentInChildren<Button>().GetComponentInChildren<TMP_Text>();
            if (text.text.Length == 0) continue;
            foreach (KeyCode modifier in Modifiers(Rows[index].Entry().Value, KeyCode.None).Reverse())
                text.text = DisplayName(modifier) + " + " + text.text;
        }
    }

    internal static void Save()
    {
        ZInput input = ZInput.instance;
        if (input == null) return;
        foreach (var row in Rows)
        {
            if (!input.m_buttons.TryGetValue(row.Name, out ZInput.ButtonDef button)) continue;
            // A key the config cannot name is left alone rather than clearing the shortcut.
            if (!TryKeyFor(button.GetActionPath(), out KeyCode key)) continue;
            ConfigEntry<KeyboardShortcut> entry = row.Entry();
            if (key == entry.Value.MainKey) continue;
            entry.Value = key == KeyCode.None
                ? KeyboardShortcut.Empty
                : new KeyboardShortcut(key, Modifiers(entry.Value, key).ToArray());
        }
    }

    // Vanilla Reset Controls clears the rows' bindings; they go back to the default keys instead.
    internal static void Reset(KeyboardMouseSettings settings)
    {
        ZInput input = ZInput.instance;
        if (input == null) return;
        foreach (var row in Rows)
            Button(input, row.Name).Rebind(PathFor(((KeyboardShortcut)row.Entry().DefaultValue).MainKey));
        settings.UpdateBindings();
    }

    // ZInput forgets its buttons when the input layout changes, so they are added back whenever the menu opens.
    private static ZInput.ButtonDef Button(ZInput input, string name)
    {
        if (input.m_buttons.TryGetValue(name, out ZInput.ButtonDef button)) return button;
        button = new ZInput.ButtonDef(name, "", null, ZInput.InputSource.KeyboardMouse, rebindable: true);
        input.m_buttons.Add(name, button);
        input.SubscribeButton(button);
        return button;
    }

    private static IEnumerable<KeyCode> Modifiers(KeyboardShortcut shortcut, KeyCode mainKey) =>
        shortcut.Modifiers.Where(modifier => modifier != mainKey);

    private static string PathFor(KeyCode key) => key == KeyCode.None ? "" : ZInput.KeyCodeToPath(key);

    // The game's own path for every key it knows, read back the other way. Paths ignore case.
    private static bool TryKeyFor(string? path, out KeyCode key)
    {
        if (string.IsNullOrEmpty(path))
        {
            key = KeyCode.None;
            return true;
        }
        if (_keyCodes == null)
        {
            _keyCodes = new Dictionary<string, KeyCode>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                if (code == KeyCode.None || code >= KeyCode.JoystickButton0) continue;
                string codePath = ZInput.KeyCodeToPath(code);
                if (!codePath.EndsWith("/None") && !_keyCodes.ContainsKey(codePath)) _keyCodes[codePath] = code;
            }
        }
        return _keyCodes.TryGetValue(path!, out key);
    }

    private static string DisplayName(KeyCode key)
    {
        string name = ZInput.KeyCodeToDisplayName(key);
        return name.StartsWith("$") ? key.ToString() : name;
    }

    // The grid fills a column before starting the next, so it needs the height for the extra rows.
    private static void FitGrid(Transform grid, int rows)
    {
        if (grid is not RectTransform rect || !grid.TryGetComponent(out GridLayoutGroup layout)) return;
        float height = layout.padding.vertical + rows * layout.cellSize.y + (rows - 1) * layout.spacing.y;
        if (rect.rect.height < height) rect.sizeDelta += new Vector2(0f, height - rect.rect.height);
    }
}
