using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Common.Networking;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain.PingTargets;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

internal static class InputManager
{
    internal static void Update(Plugin plugin)
    {
        PrefabCollectionEditor.Update();
        bool allowed = !RpcHandlers.ShouldWaitForInitialConfigSync && !PrefabCollectionEditor.BlocksInput && CanTakeInput();
        bool editorShortcutEnabled = PrefabEditorShortcutEnabled.IsOn();
        bool editorPressed = PrefabEditorShortcut.Value.IsKeyDown();
        bool editorHeld = PrefabEditorShortcut.Value.IsKeyHeld();
        bool togglePressed = !editorHeld && ToggleModShortcut.Value.IsKeyDown();
        if (!allowed) return;
        if (editorShortcutEnabled && editorPressed)
        {
            PrefabCollectionEditor.Open();
            return;
        }
        var mobHighlightPressed = MobHighlightShortcut.Value.IsKeyDown();
        if (!togglePressed && !mobHighlightPressed) return;

        if (togglePressed)
        {
            Enabled.Value = Enabled.Value.Not();

            plugin.Config.Save();
            return;
        }

        if (!Enabled.IsOn() || !mobHighlightPressed) return;

        MobTargeting.TryHighlight();
    }

    private static bool CanTakeInput()
    {
        if (!Hud.instance) return false;
        if (!Hud.instance.m_rootObject || !Hud.instance.m_rootObject.activeInHierarchy) return false;
        if (Minimap.IsOpen()) return false;

        var player = Player.m_localPlayer;
        if (!player) return false;
        if (player.IsTeleporting()) return false;
        if (player.IsSleeping()) return false;
        if (player.IsDead()) return false;
        if (player.InCutscene()) return false;

        return !IsTyping();
    }

    private static bool IsTyping() =>
        (Chat.instance && Chat.instance.HasFocus())
        || Console.IsVisible()
        || TextInput.IsVisible()
        || Menu.IsVisible()
        || InventoryGui.IsVisible()
        || StoreGui.IsVisible()
        || IsInputFieldFocused();

    private static bool IsInputFieldFocused()
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem) return false;

        var selected = eventSystem.currentSelectedGameObject;
        if (!selected) return false;

        var tmp = selected.GetComponent<TMP_InputField>();
        if (tmp && tmp.isFocused) return true;

        var legacy = selected.GetComponent<UnityEngine.UI.InputField>();
        return legacy && legacy.isFocused;
    }
}
