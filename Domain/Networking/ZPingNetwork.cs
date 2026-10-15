using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Common.Networking;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain.Networking;

internal static class ZPingNetwork
{
    internal const string LocationRpc = "JoksterCube.PingMark.ZLocationPing";
    internal const string MobRpc = "JoksterCube.PingMark.ZMobPing";
    internal const string PlayerRpc = "JoksterCube.PingMark.ZPlayerPing";
    internal const string VanillaPingMarker = "JoksterCube.PingMark.ZPingFallback";
    internal const int MaxTargetNameLength = 64;
    private static ZRoutedRpc? _registeredRpc;

    internal static void Update()
    {
        ZRoutedRpc? rpc = ZRoutedRpc.instance;
        if (rpc == _registeredRpc) return;
        _registeredRpc = rpc;
        if (rpc == null) return;

        rpc.Register<Vector3, float, Vector3, UserInfo, string>(LocationRpc, ReceiveLocation);
        rpc.Register<Vector3, float, ZDOID, UserInfo>(MobRpc, ReceiveMob);
        rpc.Register<Vector3, float, ZDOID, UserInfo>(PlayerRpc, ReceivePlayer);
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

    internal static void SendLocation(Vector3 position, string targetName)
    {
        Update();
        Player player = Player.m_localPlayer;
        if (!player) return;
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, LocationRpc,
            player.transform.position, ZPingBroadcastDistance.Value, position, UserInfo.GetLocalUser(), Truncate(targetName));
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

    private static void SendVanillaPing(Vector3 position)
    {
        if (!SendZPingsToUnmoddedPlayers.IsOn()) return;
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage",
            position, (int)Talker.Type.Ping, UserInfo.GetLocalUser(), VanillaPingMarker);
    }

    private static void ReceiveLocation(long sender, Vector3 origin, float range, Vector3 position, UserInfo user, string name)
    {
        if (CanReceive(sender, origin, range) && Chat.instance)
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