using JoksterCube.PingDistance.Common;
using BepInEx.Configuration;
using UnityEngine;

namespace JoksterCube.PingDistance.Settings;

internal static class Constants
{
    internal static class Plugin
    {
        internal const string ModName = "PingDistance";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "JoksterCube";
        internal const string ModGUID = $"{Author}.{ModName}";
        internal const string Description = "Displays player distances on world and map ping labels.";
        internal const string Copyright = "Copyright ©  2026";
        internal const string Guid = "7b25968d-61be-412f-b20f-36c18da9de86";

        internal const string ConfigFileName = $"{ModGUID}.cfg";
    }

    internal static class DebugMessages
    {
        internal const string ConfigReloadStarted = "OnConfigFileChanged called";
        internal const string ErrorLoadingConfig = $"There was an issue loading your {Plugin.ConfigFileName}";
        internal const string RequestCheckConfig = "Please check your config entries for spelling and format!";
    }

    internal static class LayerMasks
    {
        internal static readonly int MobHighlight = LayerMask.GetMask(
            "Default", "static_solid", "Default_small", "piece", "terrain", "vehicle",
            "character", "character_net", "character_ghost", "character_noenv", "item");
    }

    internal static class Groups
    {
        internal static class General
        {
            internal const string Group = "1 - General";

            internal static readonly ConfigInfo<Toggle> Enabled = new(
                Group,
                "Enabled",
                "Enable all PingDistance features.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> Lock = new(
                Group,
                "Lock Configuration",
                "If on, synchronized configuration is locked and can be changed by server admins only.",
                Toggle.On,
                true);

            internal static readonly ConfigInfo<float> RefreshInterval = new(
                Group,
                "Refresh Interval",
                new ConfigDescription(
                    "How often, in seconds, distances on visible pings and highlighted mobs are recalculated.",
                    new AcceptableValueRange<float>(0.05f, 5f)),
                0.25f,
                true);
        }

        internal static class Display
        {
            internal const string Group = "2 - Display";

            internal static readonly ConfigInfo<Toggle> UseKilometers = new(
                Group,
                "Use Kilometers",
                "Show distances of 1000 meters or more in kilometers with two decimal places.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Color> PingColor = new(
                Group,
                "Ping Color",
                "Color of in-world ping labels (name and distance).",
                Color.white,
                false);
        }

        internal static class Inputs
        {
            internal const string Group = "5 - Inputs";

            internal static readonly ConfigInfo<KeyboardShortcut> ToggleModShortcut = new(
                Group,
                "Toggle Mod Shortcut",
                "Toggle all PingDistance features.",
                new KeyboardShortcut(KeyCode.P, KeyCode.RightControl),
                false);

            internal static readonly ConfigInfo<KeyboardShortcut> MobHighlightShortcut = new(
                Group,
                "Mob Highlight Shortcut",
                "Temporarily highlight a mob under the camera crosshair, or ping the aimed location otherwise.",
                new KeyboardShortcut(KeyCode.Z),
                false);
        }

        internal static class MobHighlight
        {
            internal const string Group = "3 - Mob Highlight";

            internal static readonly ConfigInfo<float> MobHighlightDuration = new(
                Group, "Mob Highlight Duration",
                new ConfigDescription("Seconds a highlighted mob's nameplate remains visible.", new AcceptableValueRange<float>(1f, 120f)),
                10f,
                true);

            internal static readonly ConfigInfo<int> MaxHighlightedMobs = new(
                Group, "Max Highlighted Mobs",
                new ConfigDescription("Maximum number of mob nameplates highlighted at once. Highlighting another replaces the oldest.", new AcceptableValueRange<int>(1, 20)),
                1,
                true);

        }

        internal static class CameraPing
        {
            internal const string Group = "4 - Camera Ping";

            internal static readonly ConfigInfo<float> MaxDistance = new(
                Group,
                "Max Distance",
                new ConfigDescription(
                    "Maximum camera ping distance in meters. If nothing is hit, ping this far along the view direction.",
                    new AcceptableValueRange<float>(10f, 2000f)),
                500f,
                false);
        }
    }

    internal sealed class ConfigInfo<T>
    {
        internal ConfigInfo(string group, string name, string description, T defaultValue, bool synchronized = true)
            : this(group, name, new ConfigDescription(description), defaultValue, synchronized)
        {
        }

        internal ConfigInfo(string group, string name, ConfigDescription description, T defaultValue, bool synchronized = true)
        {
            Group = group;
            Name = name;
            Description = description;
            DefaultValue = defaultValue;
            Synchronized = synchronized;
        }

        internal string Group { get; }
        internal string Name { get; }
        internal ConfigDescription Description { get; }
        internal T DefaultValue { get; }
        internal bool Synchronized { get; }
    }
}
