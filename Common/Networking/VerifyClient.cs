using System;
using HarmonyLib;
using static JoksterCube.PingMark.Settings.Constants.Plugin;

namespace JoksterCube.PingMark.Common.Networking;

[HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
internal static class VerifyClient
{
    private static readonly Func<ZRoutedRpc, long> GetServerPeerId =
        AccessTools.MethodDelegate<Func<ZRoutedRpc, long>>(AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID"));

    private static void Postfix(ZNet __instance) =>
        ZRoutedRpc.instance.InvokeRoutedRPC(GetServerPeerId(ZRoutedRpc.instance), $"{ModName}RequestAdminSync", new ZPackage());
}