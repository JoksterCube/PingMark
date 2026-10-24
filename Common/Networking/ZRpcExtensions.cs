namespace JoksterCube.PingMark.Common.Networking;

internal static class ZRpcExtensions
{
    internal static string GetPeerHostName(this ZRpc rpc) => rpc.GetSocket().GetHostName();
}