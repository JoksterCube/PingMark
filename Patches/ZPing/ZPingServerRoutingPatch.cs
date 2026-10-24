using System;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Common.Networking;
using JoksterCube.PingMark.Domain;
using JoksterCube.PingMark.Domain.Networking;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Patches.ZPing;

[HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.RouteRPC))]
internal static class ZPingServerRoutingPatch
{
    private const string Feature = "Server ping routing";

    // Generous for a location name; anything larger is dropped instead of being forwarded to every peer.
    private const int MaxPackageSize = 1024;
    // A perception scan lists up to 200 object and 200 creature IDs of 12 bytes each, plus the sender's name.
    private const int MaxPerceptionPackageSize = 8192;

    private static bool Prefix(ZRoutedRpc.RoutedRPCData rpcData)
    {
        if (PatchGuard.IsDisabled(Feature)) return true;
        try { return PrefixCore(rpcData); }
        catch (Exception exception)
        {
            PatchGuard.Fail(Feature, exception);
            return true;
        }
    }

    private static bool PrefixCore(ZRoutedRpc.RoutedRPCData rpcData)
    {
        ZNet net = ZNet.instance;
        if (!net || !net.IsServer() || rpcData.m_targetPeerID != ZRoutedRpc.Everybody
            || !rpcData.m_targetZDO.IsNone()) return true;

        bool vanillaFallback = false;
        if (rpcData.m_methodHash == "ChatMessage".GetStableHashCode())
        {
            if (!TryReadFallbackMarker(rpcData.m_parameters, out bool isFallback) || !isFallback) return true;
            if (!SendZPingsToUnmoddedPlayers.IsOn()) return false;
            vanillaFallback = true;
        }
        else if (rpcData.m_methodHash != ZPingNetwork.LocationRpc.GetStableHashCode()
            && rpcData.m_methodHash != ZPingNetwork.MobRpc.GetStableHashCode()
            && rpcData.m_methodHash != ZPingNetwork.PlayerRpc.GetStableHashCode()
            && rpcData.m_methodHash != ZPingNetwork.PerceptionRpc.GetStableHashCode()) return true;

        int maxSize = rpcData.m_methodHash == ZPingNetwork.PerceptionRpc.GetStableHashCode() ? MaxPerceptionPackageSize : MaxPackageSize;
        if (rpcData.m_parameters.Size() > maxSize || !PingRateLimiter.TryConsume(rpcData.m_senderPeerID)) return false;

        Vector3 origin;
        try
        {
            rpcData.m_parameters.SetPos(0);
            origin = rpcData.m_parameters.ReadVector3();
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            rpcData.m_parameters.SetPos(0);
        }
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

    private static bool TryReadFallbackMarker(ZPackage parameters, out bool isFallback)
    {
        isFallback = false;
        try
        {
            parameters.SetPos(0);
            parameters.ReadVector3();
            int type = parameters.ReadInt();
            if (type != (int)Talker.Type.Ping) return true;
            UserInfo user = new();
            user.Deserialize(ref parameters);
            isFallback = parameters.ReadString() == ZPingNetwork.VanillaPingMarker;
            return true;
        }
        catch (Exception)
        {
            // Malformed chat data is left to vanilla routing, exactly as without the mod.
            return false;
        }
        finally
        {
            parameters.SetPos(0);
        }
    }
}
