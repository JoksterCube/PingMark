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
    internal static void Update()
    {
        PrefabCollectionEditor.Update();
        bool allowed = !RpcHandlers.ShouldWaitForInitialConfigSync && !PrefabCollectionEditor.BlocksInput && CanTakeInput();
        if (!allowed) return;
        // Modifiers match exactly, so Ctrl+P does not fire inside Ctrl+Shift+P; movement keys never block a shortcut,
        // except Perception, which needs the player standing still with nothing else held.
        if (PrefabEditorShortcutEnabled.IsOn() && PrefabEditorShortcut.Value.IsDownWhileMoving())
        {
            PrefabCollectionEditor.Open();
            return;
        }
        if (Enabled.IsOn() && FishsightShortcut.Value.IsDownWhileMoving() && HookedFish.TryToggle()) return;
        if (PerceptionShortcut.Value.IsDown())
        {
            Perception.TryActivate();
            return;
        }
        bool togglePressed = ToggleModShortcut.Value.IsDownWhileMoving();
        // Perception's modifier on the same key (Shift+Z) is sprint, so sprinting with W held still pings.
        bool pingPressed = PingShortcut.Value.IsDownWhileMoving(
            PerceptionShortcut.Value.MainKey == PingShortcut.Value.MainKey ? PerceptionShortcut.Value.Modifiers : null);
        if (!togglePressed && !pingPressed) return;

        if (togglePressed)
        {
            Enabled.Value = Enabled.Value.Not();
            return;
        }

        if (!Enabled.IsOn() || !pingPressed) return;

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
