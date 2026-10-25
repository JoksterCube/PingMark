using System.Collections.Generic;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Common.Networking;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain.Networking;

internal static class ZPingNetwork
{
    // Versioned name: clients with a different location payload ignore each other instead of misreading it.
    internal const string LocationRpc = "JoksterCube.PingMark.ZLocationPing.v3";
    internal const string MobRpc = "JoksterCube.PingMark.ZMobPing";
    internal const string PlayerRpc = "JoksterCube.PingMark.ZPlayerPing";
    internal const string PerceptionRpc = "JoksterCube.PingMark.Perception";
    internal const string VanillaPingMarker = "JoksterCube.PingMark.ZPingFallback";
    internal const int MaxTargetNameLength = 64;
    private static ZRoutedRpc? _registeredRpc;
    private static readonly Dictionary<long, (ZDOID Id, Vector3 Position)> RemoteTargets = [];

    internal static void Update()
    {
        ZRoutedRpc? rpc = ZRoutedRpc.instance;
        if (rpc == _registeredRpc) return;
        _registeredRpc = rpc;
        if (rpc == null) return;

        rpc.Register<Vector3, float, Vector3, UserInfo, string, ZDOID>(LocationRpc, ReceiveLocation);
        rpc.Register<Vector3, float, ZDOID, UserInfo>(MobRpc, ReceiveMob);
        rpc.Register<Vector3, float, ZDOID, UserInfo>(PlayerRpc, ReceivePlayer);
        rpc.Register<Vector3, float, UserInfo, ZPackage>(PerceptionRpc, ReceivePerception);
    }

    internal static bool IsInRange(Vector3 origin, Vector3 recipient, float range) =>
        range == 0f || (range > 0f && (origin - recipient).sqrMagnitude <= range * range);

    private static bool CanReceive(long sender, Vector3 origin, float range)
    {
        Player player = Player.m_localPlayer;
        if (!player || !Enabled.IsOn()) return false;
        if (sender == ZNet.GetUID()) return true;
        // The sender's range is untrusted: without a modded server both limits must pass.
        Vector3 position = player.transform.position;
        return IsInRange(origin, position, ZPingBroadcastDistance.Value)
            && (RpcHandlers.ServerHasMod || IsInRange(origin, position, range));
    }

    internal static void SendLocation(Vector3 position, string targetName, ZDOID targetId)
    {
        Update();
        Player player = Player.m_localPlayer;
        if (!player) return;
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, LocationRpc,
            player.transform.position, ZPingBroadcastDistance.Value, position, UserInfo.GetLocalUser(), Truncate(targetName), targetId);
        SendVanillaPing(position);
    }

    internal static void SendMob(ZDOID id, Vector3 position) => SendCharacter(id, position, MobRpc);

    internal static void SendPlayer(ZDOID id, Vector3 position) => SendCharacter(id, position, PlayerRpc);

    private static void SendCharacter(ZDOID id, Vector3 position, string rpcName)
    {
        Update();
        Player player = Player.m_localPlayer;
        if (!player) return;
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, rpcName,
            player.transform.position, ZPingBroadcastDistance.Value, id, UserInfo.GetLocalUser());
        SendVanillaPing(position);
    }

    // Only networked objects can be named across clients; the sender already outlined everything locally. Sent even
    // when nothing was found, so nearby players still see the Perception effect.
    internal static void SendPerception(List<ZDOID> ids, List<ZDOID> creatureIds)
    {
        Update();
        Player player = Player.m_localPlayer;
        if (!player) return;
        ZPackage targets = new();
        WriteIds(targets, ids);
        WriteIds(targets, creatureIds);
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, PerceptionRpc,
            player.transform.position, ZPingBroadcastDistance.Value, UserInfo.GetLocalUser(), targets);
    }

    private static void WriteIds(ZPackage package, List<ZDOID> ids)
    {
        package.Write(ids.Count);
        foreach (ZDOID id in ids) package.Write(id);
    }

    private static void ReceivePerception(long sender, Vector3 origin, float range, UserInfo user, ZPackage targets)
    {
        if (sender == ZNet.GetUID() || !CanReceive(sender, origin, range)) return;
        List<ZDOID> ids = [];
        List<ZDOID> creatureIds = [];
        try
        {
            ReadIds(targets, ids);
            ReadIds(targets, creatureIds);
        }
        catch (System.Exception)
        {
            // A truncated or malformed list still shows the outlines read so far.
        }
        Perception.ShowRemote(sender, user, ids, creatureIds);
    }

    private static void ReadIds(ZPackage package, List<ZDOID> ids)
    {
        int count = Mathf.Min(package.ReadInt(), PerceptionMaxTargets.Value);
        for (int index = 0; index < count; index++) ids.Add(package.ReadZDOID());
    }

    private static void SendVanillaPing(Vector3 position)
    {
        if (!SendZPingsToUnmoddedPlayers.IsOn()) return;
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage",
            position, (int)Talker.Type.Ping, UserInfo.GetLocalUser(), VanillaPingMarker);
    }

    // Consumed by the label patch when the ping's world text is created for this sender.
    internal static bool TryTakeRemoteTarget(long sender, Vector3 position, out ZDOID id)
    {
        id = ZDOID.None;
        if (!RemoteTargets.TryGetValue(sender, out var target) || (target.Position - position).sqrMagnitude > 0.01f) return false;
        RemoteTargets.Remove(sender);
        id = target.Id;
        return true;
    }

    internal static void ClearSession() => RemoteTargets.Clear();

    private static void ReceiveLocation(long sender, Vector3 origin, float range, Vector3 position, UserInfo user, string name, ZDOID targetId)
    {
        if (!CanReceive(sender, origin, range) || !Chat.instance) return;
        if (!targetId.IsNone()) RemoteTargets[sender] = (targetId, position);
        Chat.instance.OnNewChatMessage(null, sender, position, Talker.Type.Ping, user, Truncate(name));
    }

    private static string Truncate(string? name) =>
        name == null ? string.Empty : name.Length <= MaxTargetNameLength ? name : name.Substring(0, MaxTargetNameLength);

    private static void ReceiveMob(long sender, Vector3 origin, float range, ZDOID id, UserInfo user)
    {
        if (CanReceive(sender, origin, range)) MobHighlight.Highlight(sender, id, user);
    }

    private static void ReceivePlayer(long sender, Vector3 origin, float range, ZDOID id, UserInfo user)
    {
        if (CanReceive(sender, origin, range)) MobHighlight.Highlight(sender, id, user, true);
    }
}