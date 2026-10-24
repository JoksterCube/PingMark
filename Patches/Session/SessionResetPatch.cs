using System;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain;

namespace JoksterCube.PingMark.Patches.Session;

// ZNet is destroyed whenever a world or server session ends (logout, disconnect, server shutdown).
[HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
internal static class SessionResetPatch
{
    private const string Feature = "Session reset";

    private static void Postfix()
    {
        if (PatchGuard.IsDisabled(Feature)) return;
        try { PostfixCore(); }
        catch (Exception exception) { PatchGuard.Fail(Feature, exception); }
    }

    private static void PostfixCore() => SessionState.Clear();
}
