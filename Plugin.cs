using System.Reflection;
using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain;
using JoksterCube.PingMark.Domain.Networking;
using JoksterCube.PingMark.Settings;
using ServerSync;
using static JoksterCube.PingMark.Settings.Constants;
using static JoksterCube.PingMark.Settings.Constants.Plugin;

namespace JoksterCube.PingMark;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
    internal static readonly ManualLogSource ModLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
    internal static string ConnectionError = string.Empty;

    private static readonly ConfigSync ConfigSync = new(ModGUID)
    {
        DisplayName = ModName,
        CurrentVersion = ModVersion,
        MinimumRequiredVersion = ModVersion
    };

    internal static bool InitialConfigSyncDone => ConfigSync.InitialSyncDone;
    internal static bool CanEditPrefabCollections => ConfigSync.IsSourceOfTruth || ConfigSync.IsAdmin;
    internal static bool IsPrefabConfigSource => ConfigSync.IsSourceOfTruth;

    private readonly string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

    private FileSystemWatcher? _configWatcher;

    private readonly Harmony _harmony = new(ModGUID);

    private void Awake()
    {
        PluginConfig.Build(Config, ConfigSync);

        var assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        OutlineManager.Initialize();
        SetupWatcher();
    }

    private void Update()
    {
        ZPingNetwork.Update();
        MobHighlight.Update();
        InputManager.Update(this);
    }

    private void OnDestroy()
    {
        PrefabCollectionEditor.Close();
        OutlineManager.Dispose();
        _configWatcher?.Dispose();
        Config.Save();
    }

    private void OnGUI() => PrefabCollectionEditor.Draw(this);

    private void SetupWatcher()
    {
        var watcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        _configWatcher = watcher;
        watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(ConfigFileFullPath)) return;
        try
        {
            ModLogger.LogDebug(DebugMessages.ReadConfigCalled);
            Config.Reload();
        }
        catch
        {
            ModLogger.LogError(DebugMessages.ErrorLoadingConfig);
            ModLogger.LogError(DebugMessages.RequestCheckConfig);
        }
    }
}