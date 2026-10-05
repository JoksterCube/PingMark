using System;
using HarmonyLib;
using static JoksterCube.PingMark.Settings.Constants.Plugin;

namespace JoksterCube.PingMark.Common.Networking;

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
internal static class RegisterAndCheckVersion
{
    private static void Prefix(ZNetPeer peer, ref ZNet __instance)
    {
        if (!__instance.IsServer())
            RpcHandlers.BeginServerVersionCheck();

        Plugin.ModLogger.LogDebug("Registering version RPC handler");
        peer.m_rpc.Register($"{ModName}_VersionCheck",
            new Action<ZRpc, ZPackage>(RpcHandlers.RPC_ServerSyncModTemplate_Version));

        Plugin.ModLogger.LogInfo("Invoking version check");
        ZPackage zpackage = new();
        zpackage.Write(ModVersion);
        peer.m_rpc.Invoke($"{ModName}_VersionCheck", zpackage);
    }
}