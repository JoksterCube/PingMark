using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Common.Networking;
using JoksterCube.PingMark.Domain;
using JoksterCube.PingMark.Domain.Networking;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.ZPing;

[HarmonyPatch(typeof(ZRoutedRpc), "RouteRPC")]
internal static class ZPingServerRoutingPatch
{
    private static bool Prefix(ZRoutedRpc.RoutedRPCData rpcData)
    {
        ZNet net = ZNet.instance;
        if (!net || !net.IsServer() || rpcData.m_targetPeerID != ZRoutedRpc.Everybody
            || !rpcData.m_targetZDO.IsNone()) return true;

        bool vanillaFallback = false;
        if (rpcData.m_methodHash == "ChatMessage".GetStableHashCode())
        {
            rpcData.m_parameters.SetPos(0);
            rpcData.m_parameters.ReadVector3();
            int type = rpcData.m_parameters.ReadInt();
            UserInfo user = new();
            user.Deserialize(ref rpcData.m_parameters);
            string text = rpcData.m_parameters.ReadString();
            rpcData.m_parameters.SetPos(0);
            if (type != (int)Talker.Type.Ping || text != ZPingNetwork.VanillaPingMarker) return true;
            if (!SendZPingsToUnmoddedPlayers.IsOn()) return false;
            vanillaFallback = true;
        }
        else if (rpcData.m_methodHash != ZPingNetwork.LocationRpc.GetStableHashCode()
            && rpcData.m_methodHash != ZPingNetwork.MobRpc.GetStableHashCode()
            && rpcData.m_methodHash != ZPingNetwork.PlayerRpc.GetStableHashCode()) return true;

        rpcData.m_parameters.SetPos(0);
        Vector3 origin = rpcData.m_parameters.ReadVector3();
        rpcData.m_parameters.SetPos(0);
        ZNetPeer sender = net.GetPeer(rpcData.m_senderPeerID);
        if (sender != null) origin = sender.m_refPos;

        ZPackage package = new();
        rpcData.Serialize(package);
        foreach (ZNetPeer peer in net.GetConnectedPeers())
        {
            if (peer.m_uid == rpcData.m_senderPeerID || !peer.IsReady()) continue;
            bool hasMod = RpcHandlers.ValidatedPeers.Contains(peer.m_rpc);
            if (vanillaFallback ? !hasMod : hasMod && ZPingNetwork.IsInRange(origin, peer.m_refPos, ZPingBroadcastDistance.Value))
                peer.m_rpc.Invoke("RoutedRPC", package);
        }
        return false;
    }
}