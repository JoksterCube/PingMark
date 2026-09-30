using System.Reflection;
using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using JoksterCube.PingDistance.Domain;
using JoksterCube.PingDistance.Settings;
using ServerSync;
using static JoksterCube.PingDistance.Settings.Constants;
using static JoksterCube.PingDistance.Settings.Constants.Plugin;

namespace JoksterCube.PingDistance;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    private static readonly ConfigSync ConfigSync = new(ModGUID)
    {
        DisplayName = ModName,
        CurrentVersion = ModVersion,
        MinimumRequiredVersion = ModVersion
    };

    private readonly string _configFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

    private FileSystemWatcher? _configWatcher;

    private readonly Harmony _harmony = new(ModGUID);

    private void Awake()
    {
        PluginConfig.Build(Config, ConfigSync);

        var assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        SetupConfigWatcher();
    }

    private void Update()
    {
        MobHighlight.Update();
        InputManager.Update(this);
    }

    private void OnDestroy()
    {
        _configWatcher?.Dispose();
        Config.Save();
    }

    private void SetupConfigWatcher()
    {
        FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += OnConfigFileChanged;
        watcher.Created += OnConfigFileChanged;
        watcher.Renamed += OnConfigFileChanged;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        _configWatcher = watcher;
        watcher.EnableRaisingEvents = true;
    }

    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(_configFileFullPath)) return;
        try
        {
            Logger.LogDebug(DebugMessages.ConfigReloadStarted);
            Config.Reload();
        }
        catch (Exception exception)
        {
            Logger.LogError(exception);
            Logger.LogError(DebugMessages.ErrorLoadingConfig);
            Logger.LogError(DebugMessages.RequestCheckConfig);
        }
    }
}