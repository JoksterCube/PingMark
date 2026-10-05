using HarmonyLib;

namespace JoksterCube.PingMark.Common.Networking;

[HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
internal static class RemoveDisconnectedPeerFromVerified
{
    private static void Prefix(ZNetPeer peer, ref ZNet __instance)
    {
        if (!__instance.IsServer())
        {
            RpcHandlers.ResetServerVersionCheck();
            return;
        }
        Plugin.ModLogger.LogInfo($"Peer ({peer.m_rpc.GetPeerHostName()}) disconnected, removing from validated list");
        _ = RpcHandlers.ValidatedPeers.Remove(peer.m_rpc);
    }
}