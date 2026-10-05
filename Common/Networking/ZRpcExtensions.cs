using HarmonyLib;

namespace JoksterCube.PingMark.Common.Networking;

internal static class ZRpcExtensions
{
    private static readonly AccessTools.FieldRef<ZRpc, ISocket> Socket =
        AccessTools.FieldRefAccess<ZRpc, ISocket>("m_socket");

    internal static string GetPeerHostName(this ZRpc rpc) => Socket(rpc).GetHostName();
}