using HarmonyLib;
using JoksterCube.PingMark.Domain;

namespace JoksterCube.PingMark.Patches.Session;

// ZNet is destroyed whenever a world or server session ends (logout, disconnect, server shutdown).
[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
internal static class SessionResetPatch
{
    private static void Postfix() => SessionState.Clear();
}
