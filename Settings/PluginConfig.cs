using BepInEx.Configuration;
using JoksterCube.PingMark.Common;
using ServerSync;
using UnityEngine;
using static JoksterCube.PingMark.Settings.Constants.Groups;

namespace JoksterCube.PingMark.Settings;

internal static class PluginConfig
{
    private static ConfigEntry<Toggle> _serverConfigLocked = null!;

    internal static ConfigEntry<Toggle> Enabled = null!;
    internal static ConfigEntry<float> RefreshInterval = null!;
    internal static ConfigEntry<float> PingCooldown = null!;
    internal static ConfigEntry<float> ZPingBroadcastDistance = null!;
    internal static ConfigEntry<Toggle> SendZPingsToUnmoddedPlayers = null!;
    internal static ConfigEntry<Toggle> DebugPrefabId = null!;

    internal static ConfigEntry<Toggle> ShowPingerName = null!;
    internal static ConfigEntry<Toggle> ShowDistance = null!;
    internal static ConfigEntry<string> EmptyLabelText = null!;
    internal static ConfigEntry<Toggle> UseKilometers = null!;
    internal static ConfigEntry<Toggle> ShowTargetName = null!;
    internal static ConfigEntry<Color> PingColor = null!;
    internal static ConfigEntry<Toggle> MatchTextToOutline = null!;

    internal static ConfigEntry<float> CameraPingMaxDistance = null!;
    internal static ConfigEntry<float> MaxUnderwaterDepth = null!;

    internal static ConfigEntry<Toggle> DropsEnabled = null!;
    internal static ConfigEntry<Toggle> GravestonesEnabled = null!;
    internal static ConfigEntry<Toggle> PlayersEnabled = null!;
    internal static ConfigEntry<float> PlayerHighlightDuration = null!;
    internal static ConfigEntry<Toggle> HideOwnPlayerOutline = null!;
    internal static ConfigEntry<Toggle> OthersEnabled = null!;

    internal static ConfigEntry<Toggle> PingOutlineEnabled = null!;
    internal static ConfigEntry<float> OutlineWidth = null!;
    internal static ConfigEntry<Color> PingDropColor = null!;
    internal static ConfigEntry<Color> PingPlayerColor = null!;
    internal static ConfigEntry<Color> PingAnythingColor = null!;
    internal static ConfigEntry<Color> HostileOutlineColor = null!;
    internal static ConfigEntry<Color> AllyOutlineColor = null!;
    internal static ConfigEntry<Color>[] PrefabSectionColors = [];
    internal static ConfigEntry<int> UnassignedSectionColorId = null!;

    internal static ConfigEntry<Toggle> MobHighlightEnabled = null!;
    internal static ConfigEntry<Toggle> MobOutlineEnabled = null!;
    internal static ConfigEntry<float> MobHighlightDuration = null!;
    internal static ConfigEntry<int> MaxHighlightedMobs = null!;

    internal static ConfigEntry<KeyboardShortcut> ToggleModShortcut = null!;
    internal static ConfigEntry<KeyboardShortcut> PrefabEditorShortcut = null!;
    internal static ConfigEntry<KeyboardShortcut> PingShortcut = null!;
    internal static ConfigEntry<Toggle> PrefabEditorShortcutEnabled = null!;
    internal static ConfigEntry<KeyboardShortcut> PerceptionShortcut = null!;

    internal static ConfigEntry<Toggle> PerceptionEnabled = null!;
    internal static ConfigEntry<float> PerceptionRange = null!;
    internal static ConfigEntry<int> PerceptionMaxTargets = null!;
    internal static ConfigEntry<float> PerceptionFatigueDuration = null!;
    internal static ConfigEntry<Toggle> PerceptionCreaturesEnabled = null!;

    internal static void Build(ConfigFile config, ConfigSync configSync)
    {
        ConfigOptions.Initialize(config, configSync);

        Enabled = ConfigOptions.Config(General.Enabled);
        _serverConfigLocked = ConfigOptions.Config(General.Lock);
        configSync.AddLockingConfigEntry(_serverConfigLocked);
        RefreshInterval = ConfigOptions.Config(General.RefreshInterval);
        PingCooldown = ConfigOptions.Config(General.PingCooldown);
        ZPingBroadcastDistance = ConfigOptions.Config(General.ZPingBroadcastDistance);
        SendZPingsToUnmoddedPlayers = ConfigOptions.Config(General.SendZPingsToUnmoddedPlayers);
        DebugPrefabId = ConfigOptions.Config(General.DebugPrefabId);

        ShowPingerName = ConfigOptions.Config(PingLabel.ShowPingerName);
        ShowDistance = ConfigOptions.Config(PingLabel.ShowDistance);
        EmptyLabelText = ConfigOptions.Config(PingLabel.EmptyLabelText);
        UseKilometers = ConfigOptions.Config(PingLabel.UseKilometers);
        ShowTargetName = ConfigOptions.Config(PingLabel.ShowTargetName);
        PingColor = ConfigOptions.Config(PingLabel.PingColor);
        MatchTextToOutline = ConfigOptions.Config(PingLabel.MatchTextToOutline);

        CameraPingMaxDistance = ConfigOptions.Config(CameraPing.MaxDistance);
        MaxUnderwaterDepth = ConfigOptions.Config(CameraPing.MaxUnderwaterDepth);

        DropsEnabled = ConfigOptions.Config(PingTargets.DropsEnabled);
        GravestonesEnabled = ConfigOptions.Config(PingTargets.GravestonesEnabled);
        PlayersEnabled = ConfigOptions.Config(PingTargets.PlayersEnabled);
        PlayerHighlightDuration = ConfigOptions.Config(PlayerHighlight.Duration);
        HideOwnPlayerOutline = ConfigOptions.Config(PlayerHighlight.HideOwnOutline);
        OthersEnabled = ConfigOptions.Config(PingTargets.OthersEnabled);

        PingOutlineEnabled = ConfigOptions.Config(Outlines.PingOutlineEnabled);
        OutlineWidth = ConfigOptions.Config(Outlines.OutlineWidth);
        PingDropColor = ConfigOptions.Config(Outlines.PingDropColor);
        PingPlayerColor = ConfigOptions.Config(Outlines.PingPlayerColor);
        PingAnythingColor = ConfigOptions.Config(Outlines.PingAnythingColor);
        HostileOutlineColor = ConfigOptions.Config(Outlines.HostileOutlineColor);
        AllyOutlineColor = ConfigOptions.Config(Outlines.AllyOutlineColor);
        MigrateAllyOutlineColor();
        PrefabSectionColors =
        [
            ConfigOptions.Config(PrefabCollections.Color1),
            ConfigOptions.Config(PrefabCollections.Color2),
            ConfigOptions.Config(PrefabCollections.Color3),
            ConfigOptions.Config(PrefabCollections.Color4),
            ConfigOptions.Config(PrefabCollections.Color5),
            ConfigOptions.Config(PrefabCollections.Color6),
            ConfigOptions.Config(PrefabCollections.Color7),
            ConfigOptions.Config(PrefabCollections.Color8),
            ConfigOptions.Config(PrefabCollections.Color9),
            ConfigOptions.Config(PrefabCollections.Color10),
            ConfigOptions.Config(PrefabCollections.Color11),
            ConfigOptions.Config(PrefabCollections.Color12)
        ];
        UnassignedSectionColorId = ConfigOptions.Config(PrefabCollections.UnassignedColorId);

        MobHighlightEnabled = ConfigOptions.Config(MobHighlight.Enabled);
        MobOutlineEnabled = ConfigOptions.Config(MobHighlight.OutlineEnabled);
        MobHighlightDuration = ConfigOptions.Config(MobHighlight.MobHighlightDuration);
        MaxHighlightedMobs = ConfigOptions.Config(MobHighlight.MaxHighlightedMobs);

        ToggleModShortcut = ConfigOptions.Config(Inputs.ToggleModShortcut);
        PrefabEditorShortcut = ConfigOptions.Config(Inputs.PrefabEditorShortcut);
        PingShortcut = ConfigOptions.Config(Inputs.PingShortcut);
        MigrateRenamedShortcut(config);
        PerceptionShortcut = ConfigOptions.Config(Inputs.PerceptionShortcut);
        PrefabEditorShortcutEnabled = ConfigOptions.Config(Inputs.PrefabEditorShortcutEnabled);

        PerceptionEnabled = ConfigOptions.Config(Perception.Enabled);
        PerceptionRange = ConfigOptions.Config(Perception.Range);
        PerceptionMaxTargets = ConfigOptions.Config(Perception.MaxTargets);
        PerceptionFatigueDuration = ConfigOptions.Config(Perception.FatigueDuration);
        PerceptionCreaturesEnabled = ConfigOptions.Config(Perception.CreaturesEnabled);
    }

    // Untouched configs still hold an earlier default (pale green before 1.2.0, blue in 1.2.0); move them to
    // the health bar green. A color the player picked is kept.
    private static void MigrateAllyOutlineColor()
    {
        // The config file stores 8-bit hex, so compare at that precision.
        Color32 value = AllyOutlineColor.Value;
        if (SameColor(value, new Color(0.5f, 0.85f, 0.3f)) || SameColor(value, new Color(0.3f, 0.65f, 1f)))
            AllyOutlineColor.Value = Outlines.AllyOutlineColor.DefaultValue;
    }

    private static bool SameColor(Color32 left, Color32 right) =>
        left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a;

    // Keeps a player's custom key from the old "Mob Highlight Shortcut" entry and drops the old entry.
    private static void MigrateRenamedShortcut(ConfigFile config)
    {
        ConfigEntry<KeyboardShortcut> legacy = config.Bind(Inputs.Group, Inputs.LegacyPingShortcutName, Inputs.PingShortcut.DefaultValue);
        if (!legacy.Value.Equals(Inputs.PingShortcut.DefaultValue)) PingShortcut.Value = legacy.Value;
        config.Remove(legacy.Definition);
        config.Save();
    }

    internal static Color GetPrefabSectionColor(int colorId)
    {
        if (PrefabSectionColors.Length == 0) return Color.white;
        int index = Mathf.Clamp(colorId - 1, 0, PrefabSectionColors.Length - 1);
        return PrefabSectionColors[index].Value;
    }
}