using JoksterCube.AbilityKit;
using JoksterCube.PingMark.Common;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

// The fish on the local player's line, outlined until it is caught or gets away. A Fishsight slot beside the
// Guardian Power shows while it is hooked: a switch with no cooldown, a red cross over its icon when off, switched by
// Fishsight Shortcut for this fish only. Each new catch starts on if it was pinged just before it bit (so the
// outline carries on) or if Auto Outline Hooked Fish is on; otherwise it starts off until the player switches it on.
internal static class HookedFish
{
    private const string Name = "Fishsight";
    private static readonly AbilitySlot Slot = new("PingMark_FishOutline", SlotPriority.PingMarkFishsight);

    // The fish on the local player's line this frame, or null.
    internal static Fish? Current { get; private set; }

    internal static Color Color => PingFishColor.Value;

    // Whether the fish on the line now is outlined; decided when it is hooked, then switched by the player.
    private static bool _on;

    internal static bool ShowOutline => Current && _on;

    internal static void Update()
    {
        Fish? fish = Enabled.IsOn() ? Find() : null;
        if (fish && fish != Current) _on = OutlineManager.IsPinged(fish!.gameObject) || AutoOutlineHookedFish.IsOn();
        Current = fish;
    }

    // Only while a fish is hooked, so the shortcut does nothing the rest of the time.
    internal static bool TryToggle()
    {
        if (!Current || !HasShortcut) return false;
        _on = !_on;
        return true;
    }

    internal static void UpdateHud(Hud hud)
    {
        Slot.Update(hud, Current && HasShortcut, FishTagIcon.Get(), Name, _on ? "On" : "Off", ready: true, crossed: !_on);
    }

    // Without a key there is no way to switch it, so the slot stays hidden.
    private static bool HasShortcut => FishsightShortcut.Value.MainKey != KeyCode.None;

    // The float the local player cast; its catch is synced in its ZDO.
    private static Fish? Find()
    {
        Player player = Player.m_localPlayer;
        if (!player) return null;
        long me = player.GetZDOID().UserID;
        foreach (FishingFloat fishingFloat in FishingFloat.GetAllInstances())
        {
            if (!fishingFloat || !fishingFloat.m_nview || !fishingFloat.m_nview.IsValid()) continue;
            if (fishingFloat.m_nview.GetZDO().GetLong(ZDOVars.s_rodOwner) != me) continue;
            Fish fish = fishingFloat.GetCatch();
            if (fish) return fish;
        }
        return null;
    }
}
