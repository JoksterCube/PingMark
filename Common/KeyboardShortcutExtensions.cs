using BepInEx.Configuration;
using UnityEngine;
using System.Linq;

namespace JoksterCube.PingMark.Common;

internal static class KeyboardShortcutExtensions
{
    internal static bool IsKeyDown(this KeyboardShortcut shortcut) =>
        shortcut.MainKey != KeyCode.None && Input.GetKeyDown(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);

    internal static bool IsKeyHeld(this KeyboardShortcut shortcut) =>
        shortcut.MainKey != KeyCode.None && Input.GetKey(shortcut.MainKey) && shortcut.Modifiers.All(Input.GetKey);
}