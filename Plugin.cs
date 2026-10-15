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

    private readonly string _configFileFullPath = Path.Combine(Paths.ConfigPath, ConfigFileName);

    private FileSystemWatcher? _configWatcher;
    private DateTime _lastConfigWrite;

    private readonly Harmony _harmony = new(ModGUID);

    private void Awake()
    {
        PluginConfig.Build(Config, ConfigSync);
        PrefabCollectionStore.Initialize(Config);
        PrefabCollectionEditor.View = PrefabCollectionEditorView.Create(this);

        var assembly = Assembly.GetExecutingAssembly();
        _harmony.PatchAll(assembly);
        OutlineManager.Initialize();
        SetupWatcher();
    }

    private void Update()
    {
        ZPingNetwork.Update();
        MobHighlight.Update();
        InputManager.Update();
    }

    private void OnDestroy()
    {
        PrefabCollectionEditor.Close();
        OutlineManager.Dispose();
        _harmony.UnpatchSelf();
        PrefabCollectionStore.Dispose();
        _configWatcher?.Dispose();
        Config.Save();
    }

    private void SetupWatcher()
    {
        var watcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        _configWatcher = watcher;
        watcher.EnableRaisingEvents = true;
    }

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        if (!File.Exists(_configFileFullPath)) return;
        // The watcher usually raises several events per save; reload once per actual write.
        DateTime lastWrite = File.GetLastWriteTimeUtc(_configFileFullPath);
        if (lastWrite == _lastConfigWrite) return;
        _lastConfigWrite = lastWrite;
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