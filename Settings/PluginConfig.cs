using BepInEx.Configuration;
using JoksterCube.PingDistance.Common;
using ServerSync;
using UnityEngine;
using static JoksterCube.PingDistance.Settings.Constants.Groups;

namespace JoksterCube.PingDistance.Settings;

internal static class PluginConfig
{
    private static ConfigEntry<Toggle> _serverConfigLocked = null!;

    internal static ConfigEntry<Toggle> Enabled = null!;
    internal static ConfigEntry<Toggle> UseKilometers = null!;
    internal static ConfigEntry<Color> PingColor = null!;
    internal static ConfigEntry<float> MobHighlightDuration = null!;
    internal static ConfigEntry<int> MaxHighlightedMobs = null!;
    internal static ConfigEntry<float> RefreshInterval = null!;
    internal static ConfigEntry<KeyboardShortcut> ToggleModShortcut = null!;
    internal static ConfigEntry<KeyboardShortcut> MobHighlightShortcut = null!;
    internal static ConfigEntry<float> CameraPingMaxDistance = null!;

    internal static void Build(ConfigFile config, ConfigSync configSync)
    {
        ConfigOptions.Initialize(config, configSync);

        _serverConfigLocked = ConfigOptions.Config(General.Lock);
        configSync.AddLockingConfigEntry(_serverConfigLocked);

        Enabled = ConfigOptions.Config(General.Enabled);
        UseKilometers = ConfigOptions.Config(Constants.Groups.Display.UseKilometers);
        PingColor = ConfigOptions.Config(Constants.Groups.Display.PingColor);
        MobHighlightDuration = ConfigOptions.Config(MobHighlight.MobHighlightDuration);
        MaxHighlightedMobs = ConfigOptions.Config(MobHighlight.MaxHighlightedMobs);
        RefreshInterval = ConfigOptions.Config(General.RefreshInterval);
        ToggleModShortcut = ConfigOptions.Config(Inputs.ToggleModShortcut);
        MobHighlightShortcut = ConfigOptions.Config(Inputs.MobHighlightShortcut);
        CameraPingMaxDistance = ConfigOptions.Config(CameraPing.MaxDistance);
    }
}