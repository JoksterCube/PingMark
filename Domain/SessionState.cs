using JoksterCube.PingMark.Common.Networking;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain.Networking;
using JoksterCube.PingMark.Domain.PingTargets;

namespace JoksterCube.PingMark.Domain;

// Single place that forgets everything tied to one world/server connection.
internal static class SessionState
{
    internal static void Clear()
    {
        PrefabCollectionEditor.ClearSession();
        RpcHandlers.ClearSession();
        PingRateLimiter.Clear();
        ZPingNetwork.ClearSession();
        MobHighlight.Clear();
        MobTargeting.Clear();
        Perception.Clear();
        OutlineManager.ClearSession();
        GroundPingLabelAnchors.Anchors.Clear();
        PingLabelFormatter.Clear();
    }
}
