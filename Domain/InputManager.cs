using JoksterCube.PingDistance.Common;
using TMPro;
using UnityEngine.EventSystems;
using static JoksterCube.PingDistance.Settings.PluginConfig;

namespace JoksterCube.PingDistance.Domain;

internal static class InputManager
{
    internal static void Update(Plugin plugin)
    {
        var toggleModPressed = ToggleModShortcut.Value.IsKeyDown();
        var mobHighlightPressed = MobHighlightShortcut.Value.IsKeyDown();
        if (!toggleModPressed && !mobHighlightPressed) return;
        if (!CanTakeInput()) return;

        if (toggleModPressed)
        {
            Enabled.Value = Enabled.Value == Toggle.On ? Toggle.Off : Toggle.On;

            plugin.Config.Save();
            return;
        }

        if (Enabled.Value != Toggle.On || !mobHighlightPressed) return;

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
