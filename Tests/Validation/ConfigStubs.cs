namespace JoksterCube.PingMark.Settings;

internal class Setting<T> { internal T Value; public Setting(T value) { Value = value; } }
internal static class PluginConfig
{
    internal static Setting<bool> Enabled = new Setting<bool>(true);
    internal static Setting<bool> SendZPingsToUnmoddedPlayers = new Setting<bool>(true);
    internal static Setting<float> ZPingBroadcastDistance = new Setting<float>(200);
}