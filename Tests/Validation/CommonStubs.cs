using System.Collections.Generic;
using JoksterCube.PingMark.Settings;

namespace JoksterCube.PingMark.Common;

internal static class RpcHandlers
{
    internal static bool ServerHasMod = true;
    internal static readonly HashSet<ZRpc> ValidatedPeers = new HashSet<ZRpc>();
}
internal static class ToggleExtensions
{
    internal static bool IsOn(this Setting<bool> setting) => setting.Value;
}