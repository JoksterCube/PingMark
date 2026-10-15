using System;
using System.Collections.Generic;
using System.Reflection;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;
using JoksterCube.PingMark.Settings;
using JoksterCube.PingMark.Validation.UnityEngine;

namespace JoksterCube.PingMark.Validation;

public static class ZPingNetworkChecks
{
    private static int checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        checks++;
    }
    private static bool Route(string method, string text = "", int type = 3)
    {
        foreach (ZNetPeer peer in ZNet.instance.Peers) peer.m_rpc.Sent = 0;
        var data = new ZRoutedRpc.RoutedRPCData { m_senderPeerID = 1, m_methodHash = method.GetStableHashCode() };
        data.m_parameters.Values.Add(new Vector3(0));
        if (method == "ChatMessage") data.m_parameters.Values.AddRange(new object[] { type, "Name", "UserID", text });
        return (bool)typeof(ZPingServerRoutingPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { data });
    }
    public static string Run()
    {
        checks = 0;
        ZNet.instance = new ZNet();
        ZRoutedRpc.instance = new ZRoutedRpc();
        RpcHandlers.ValidatedPeers.Clear();
        PluginConfig.Enabled.Value = true;
        PluginConfig.SendZPingsToUnmoddedPlayers.Value = true;
        PluginConfig.ZPingBroadcastDistance.Value = 200;
        var sender = new ZNetPeer { m_uid = 1 };
        var near = new ZNetPeer { m_uid = 2, m_refPos = new Vector3(200) };
        var far = new ZNetPeer { m_uid = 3, m_refPos = new Vector3(201) };
        var vanilla = new ZNetPeer { m_uid = 4, m_refPos = new Vector3(10000) };
        var unready = new ZNetPeer { m_uid = 5, Ready = false };
        ZNet.instance.Peers.AddRange(new[] { sender, near, far, vanilla, unready });
        RpcHandlers.ValidatedPeers.Add(near.m_rpc);
        RpcHandlers.ValidatedPeers.Add(far.m_rpc);
        foreach (string method in new[] { ZPingNetwork.LocationRpc, ZPingNetwork.MobRpc, ZPingNetwork.PlayerRpc })
        {
            Check(!Route(method), "custom route intercepted");
            Check(near.m_rpc.Sent == 1 && far.m_rpc.Sent == 0 && vanilla.m_rpc.Sent == 0, "custom range and mod detection");
            Check(sender.m_rpc.Sent == 0 && unready.m_rpc.Sent == 0, "sender and unready excluded");
        }
        Check(!Route("ChatMessage", ZPingNetwork.VanillaPingMarker), "fallback intercepted");
        Check(vanilla.m_rpc.Sent == 1 && near.m_rpc.Sent == 0 && far.m_rpc.Sent == 0, "distant vanilla only");
        Check(sender.m_rpc.Sent == 0 && unready.m_rpc.Sent == 0, "fallback sender and unready excluded");
        PluginConfig.SendZPingsToUnmoddedPlayers.Value = false;
        Check(!Route("ChatMessage", ZPingNetwork.VanillaPingMarker) && vanilla.m_rpc.Sent == 0, "toggle blocks fallback");
        Check(Route("ChatMessage"), "ordinary map ping unchanged");
        Check(Route("ChatMessage", ZPingNetwork.VanillaPingMarker, 2), "ordinary chat unchanged");
        PluginConfig.ZPingBroadcastDistance.Value = 0;
        Route(ZPingNetwork.LocationRpc);
        Check(near.m_rpc.Sent == 1 && far.m_rpc.Sent == 1 && vanilla.m_rpc.Sent == 0, "unlimited custom range");
        var receive = typeof(ZPingVanillaFallbackPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
        Check(!(bool)receive.Invoke(null, new object[] { 3, ZPingNetwork.VanillaPingMarker }), "modded duplicate suppressed");
        Check((bool)receive.Invoke(null, new object[] { 3, "" }), "ordinary ping received");
        ZPingNetwork.SendLocation(new Vector3(5000), "Target");
        Check(ZRoutedRpc.instance.Sent.Count == 1, "toggle off sends only custom");
        PluginConfig.SendZPingsToUnmoddedPlayers.Value = true;
        ZRoutedRpc.instance.Sent.Clear();
        ZPingNetwork.SendLocation(new Vector3(5000), "Target");
        ZPingNetwork.SendMob(new ZDOID(), new Vector3(6000));
        ZPingNetwork.SendPlayer(new ZDOID(), new Vector3(7000));
        Check(ZRoutedRpc.instance.Sent.Count == 6, "location mob and player send companion pings");
        Check(ZPingNetwork.IsInRange(new Vector3(0), new Vector3(200), 200), "range boundary included");
        Check(!ZPingNetwork.IsInRange(new Vector3(0), new Vector3(201), 200), "outside range excluded");
        return checks + " routing checks passed";
    }
}
