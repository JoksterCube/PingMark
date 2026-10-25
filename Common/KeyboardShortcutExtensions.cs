using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace JoksterCube.PingMark.Common;

internal static class KeyboardShortcutExtensions
{
    private static readonly KeyCode[] ModifierKeys =
    [
        KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl,
        KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.AltGr, KeyCode.LeftCommand, KeyCode.RightCommand,
        KeyCode.LeftWindows, KeyCode.RightWindows
    ];

    // KeyboardShortcut.IsDown fails while any other key is held, so walking (WASD) blocks every shortcut. Only
    // extra modifiers block here: Ctrl+P still does not fire inside Ctrl+Shift+P, but P fires while running.
    // Allowed modifiers do not block either, so Z still pings while sprinting with Shift held.
    internal static bool IsDownWhileMoving(this KeyboardShortcut shortcut, IEnumerable<KeyCode>? allowedModifiers = null)
    {
        KeyCode mainKey = shortcut.MainKey;
        if (mainKey == KeyCode.None || !Input.GetKeyDown(mainKey)) return false;
        KeyCode[] modifiers = shortcut.Modifiers.ToArray();
        if (!modifiers.All(Input.GetKey)) return false;
        KeyCode[] allowed = allowedModifiers?.ToArray() ?? [];
        return ModifierKeys.All(key => key == mainKey || modifiers.Contains(key) || allowed.Contains(key) || !Input.GetKey(key));
    }
}
