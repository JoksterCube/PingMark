using System.Collections.Generic;
using UnityEngine;
using static JoksterCube.PingMark.Settings.Constants.Plugin;

namespace JoksterCube.PingMark.Common.Networking;

internal static class RpcHandlers
{
    internal static readonly HashSet<ZRpc> ValidatedPeers = [];
    private static bool _waitingForServerVersion;
    private static bool _serverVersionReceived;
    private static float _serverVersionCheckStartedAt;

    internal static bool ServerHasMod => ZNet.instance && (ZNet.instance.IsServer() || _serverVersionReceived);

    internal static bool ShouldWaitForInitialConfigSync
    {
        get
        {
            if (!ZNet.instance || ZNet.instance.IsServer() || !_waitingForServerVersion) return false;
            if (_serverVersionReceived) return !Plugin.InitialConfigSyncDone;

            return Time.realtimeSinceStartup - _serverVersionCheckStartedAt < 1f;
        }
    }

    internal static void BeginServerVersionCheck()
    {
        _waitingForServerVersion = true;
        _serverVersionReceived = false;
        _serverVersionCheckStartedAt = Time.realtimeSinceStartup;
    }

    internal static void ResetServerVersionCheck()
    {
        _waitingForServerVersion = false;
        _serverVersionReceived = false;
    }

    internal static void RPC_ServerSyncModTemplate_Version(ZRpc rpc, ZPackage pkg)
    {
        string version = pkg.ReadString();
        var instance = ZNet.instance;
        if (!instance) return;

        var isServer = instance.IsServer();
        if (!isServer)
            _serverVersionReceived = true;

        Plugin.ModLogger.LogInfo($"Version check, local: {ModVersion},  remote: {version}");
        if (version != ModVersion)
        {
            Plugin.ConnectionError = $"{ModName} Installed: {ModVersion}\n Needed: {version}";
            if (!isServer) return;
            Plugin.ModLogger.LogWarning($"Peer ({rpc.GetPeerHostName()}) has incompatible version, disconnecting...");
            rpc.Invoke("Error", 3);
        }
        else if (!isServer)
        {
            Plugin.ModLogger.LogInfo("Received same version from server!");
        }
        else
        {
            Plugin.ModLogger.LogInfo($"Adding peer ({rpc.GetPeerHostName()}) to validated list");
            ValidatedPeers.Add(rpc);
        }
    }
}