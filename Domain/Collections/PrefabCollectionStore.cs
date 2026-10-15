using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using JoksterCube.PingMark.Common.Networking;
using static JoksterCube.PingMark.Settings.Constants.Plugin;
using TargetLists = JoksterCube.PingMark.Settings.Constants.Groups.PingTargets;

namespace JoksterCube.PingMark.Domain.Collections;

// Owns the prefab collection YAML. The server's file is authoritative: clients on a modded server only
// receive it, and edits are sent to the server, which accepts them from admins only.
internal static class PrefabCollectionStore
{
    internal const string ValueRpc = "JoksterCube.PingMark.Collections";
    internal const string SubmitRpc = "JoksterCube.PingMark.SubmitCollections";

    private static readonly string FilePath = Path.Combine(Paths.ConfigPath, CollectionsFileName);
    private static FileSystemWatcher? _watcher;
    private static string _localValue = string.Empty;
    private static string? _serverValue;
    private static DateTime _lastFileWrite;

    private static bool UsesServerValue => RpcHandlers.ServerHasMod && ZNet.instance && !ZNet.instance.IsServer();

    internal static string Value => UsesServerValue ? _serverValue ?? string.Empty : _localValue;

    // On a modded server the collections must arrive before they can be edited.
    internal static bool IsReady => !UsesServerValue || _serverValue != null;

    internal static void Initialize(ConfigFile config)
    {
        MigrateFromConfig(config);
        _localValue = ReadFile();

        _watcher = new FileSystemWatcher(Paths.ConfigPath, CollectionsFileName);
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Renamed += OnFileChanged;
        _watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        _watcher.EnableRaisingEvents = true;
    }

    internal static void Dispose()
    {
        _watcher?.Dispose();
        _watcher = null;
    }

    internal static void RegisterRpcs(ZRpc rpc)
    {
        rpc.Register<string>(ValueRpc, RPC_Value);
        rpc.Register<string>(SubmitRpc, RPC_Submit);
    }

    internal static void ResetServerValue() => _serverValue = null;

    internal static void SendTo(ZRpc rpc) => rpc.Invoke(ValueRpc, _localValue);

    internal static void Submit(string value)
    {
        if (UsesServerValue)
        {
            ZRpc? server = ZNet.instance.GetServerRPC();
            server?.Invoke(SubmitRpc, value);
            return;
        }
        SetLocal(value);
    }

    private static void SetLocal(string value)
    {
        if (value == _localValue) return;
        _localValue = value;
        WriteFile(value);
        Broadcast();
    }

    private static void Broadcast()
    {
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        foreach (ZRpc rpc in RpcHandlers.ValidatedPeers) SendTo(rpc);
    }

    private static void RPC_Value(ZRpc rpc, string value)
    {
        if (ZNet.instance && !ZNet.instance.IsServer()) _serverValue = value;
    }

    private static void RPC_Submit(ZRpc rpc, string value)
    {
        ZNet net = ZNet.instance;
        if (!net || !net.IsServer()) return;
        string hostName = rpc.GetSocket().GetHostName();
        if (!net.IsAdmin(hostName))
        {
            Plugin.ModLogger.LogWarning($"Rejected prefab collection changes from non-admin {hostName}.");
            SendTo(rpc);
            return;
        }
        Plugin.ModLogger.LogInfo($"Prefab collections updated by admin {hostName}.");
        SetLocal(value);
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // The watcher raises several events per save (including our own); handle each actual write once.
        DateTime lastWrite = GetLastWrite();
        if (lastWrite == _lastFileWrite) return;
        _lastFileWrite = lastWrite;
        string value = ReadFile();
        if (value == _localValue) return;
        _localValue = value;
        Broadcast();
    }

    private static string ReadFile()
    {
        try
        {
            return File.Exists(FilePath) ? File.ReadAllText(FilePath) : TargetLists.PrefabSections.DefaultValue;
        }
        catch (IOException exception)
        {
            Plugin.ModLogger.LogError($"Unable to read {CollectionsFileName}: {exception.Message}");
            return _localValue;
        }
    }

    private static void WriteFile(string value)
    {
        try
        {
            File.WriteAllText(FilePath, value);
            _lastFileWrite = GetLastWrite();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Plugin.ModLogger.LogError($"Unable to save {CollectionsFileName}: {exception.Message}");
        }
    }

    private static DateTime GetLastWrite() => File.Exists(FilePath) ? File.GetLastWriteTimeUtc(FilePath) : default;

    // Collections used to live in the main .cfg as a single synced entry; move them to their own file once.
    private static void MigrateFromConfig(ConfigFile config)
    {
        if (File.Exists(FilePath)) return;
        ConfigEntry<string> legacy = config.Bind(TargetLists.Group, TargetLists.PrefabSections.Name,
            TargetLists.PrefabSections.DefaultValue, TargetLists.PrefabSections.Description);
        string value = string.IsNullOrWhiteSpace(legacy.Value) ? TargetLists.PrefabSections.DefaultValue : legacy.Value;
        config.Remove(legacy.Definition);
        config.Save();
        WriteFile(value);
    }
}
