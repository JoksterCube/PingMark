using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using ServerSync;

namespace JoksterCube.PingMark.Settings;

internal static class ConfigOptions
{
    private static ConfigFile? _configFile;
    private static ConfigSync? _configSync;

    internal static void Initialize(ConfigFile config, ConfigSync configSync)
    {
        _configFile = config;
        _configSync = configSync;
    }

    internal static ConfigEntry<T> Config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
    {
        ConfigDescription extendedDescription = new(
            description.Description + (synchronizedSetting
                ? " [Synced with Server]"
                : " [Not Synced with Server]"),
            description.AcceptableValues,
            description.Tags);

        ConfigEntry<T> configEntry = (_configFile ?? throw new InvalidOperationException("ConfigOptions is not initialized.")).Bind(group, name, value, extendedDescription);
        SyncedConfigEntry<T> syncedConfigEntry = (_configSync ?? throw new InvalidOperationException("ConfigOptions is not initialized.")).AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;
        return configEntry;
    }

    internal static ConfigEntry<T> Config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) =>
        Config(group, name, value, new ConfigDescription(description), synchronizedSetting);

    internal static ConfigEntry<T> Config<T>(ConfigInfo<T> configInfo) =>
        Config(configInfo.Group, configInfo.Name, configInfo.DefaultValue, configInfo.Description, configInfo.Synchronized);

    internal static ConfigEntry<T> Config<T>(ConfigInfo<T> configInfo, string legacyGroup)
    {
        ConfigFile config = _configFile ?? throw new InvalidOperationException("ConfigOptions is not initialized.");
        ConfigEntry<T> legacyEntry = config.Bind(legacyGroup, configInfo.Name, configInfo.DefaultValue, configInfo.Description);
        T legacyValue = legacyEntry.Value;
        config.Remove(legacyEntry.Definition);

        ConfigEntry<T> configEntry = Config(configInfo);
        if (!EqualityComparer<T>.Default.Equals(legacyValue, configInfo.DefaultValue))
            configEntry.Value = legacyValue;

        return configEntry;
    }
}