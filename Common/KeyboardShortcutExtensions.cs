using BepInEx.Configuration;
using UnityEngine;

namespace JoksterCube.PingDistance.Common;

internal static class KeyboardShortcutExtensions
{
    internal static bool IsKeyDown(this KeyboardShortcut shortcut)
    {
        if (shortcut.MainKey == KeyCode.None || !Input.GetKeyDown(shortcut.MainKey)) return false;

        foreach (KeyCode modifier in shortcut.Modifiers)
        {
            if (!Input.GetKey(modifier)) return false;
        }

        return true;
    }
}