using JoksterCube.AbilityKit;
using JoksterCube.PingMark.Common;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

// The Perception slot beside the Guardian Power (see AbilityKit): its name, Ready or the time until it can be used
// again. Other mods' ability slots line up with it, by priority.
internal static class PerceptionHud
{
    private const string Name = "Perception";
    private static readonly AbilitySlot Slot = new("PingMark_Perception", SlotPriority.PingMarkPerception);

    internal static void Update(Hud hud)
    {
        float cooldown = Perception.Cooldown;
        Slot.Update(hud, Enabled.IsOn() && PerceptionEnabled.IsOn(), Perception.Icon, Name,
            cooldown > 0f ? AbilitySlot.TimeText(cooldown) : AbilitySlot.ReadyText, ready: cooldown <= 0f);
    }
}
