using System;
using HarmonyLib;
using JoksterCube.PingMark.Domain.Collections;
using static JoksterCube.PingMark.Settings.Constants.Plugin;

namespace JoksterCube.PingMark.Common.Networking;

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
internal static class RegisterAndCheckVersion
{
    private static void Prefix(ZNetPeer peer, ZNet __instance)
    {
        if (!__instance.IsServer())
            RpcHandlers.BeginServerVersionCheck();

        Plugin.ModLogger.LogDebug("Registering version RPC handler");
        peer.m_rpc.Register($"{ModName}_VersionCheck",
            new Action<ZRpc, ZPackage>(RpcHandlers.RPC_VersionCheck));
        PrefabCollectionStore.RegisterRpcs(peer.m_rpc);

        Plugin.ModLogger.LogInfo("Invoking version check");
        ZPackage zpackage = new();
        // Only the version: 1.2.0 read a missing protocol number as an old protocol, so it rejects newer clients too.
        zpackage.Write(ModVersion);
        peer.m_rpc.Invoke($"{ModName}_VersionCheck", zpackage);
    }
}