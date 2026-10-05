using HarmonyLib;
using JoksterCube.PingMark.Domain.Networking;

namespace JoksterCube.PingMark.Patches.ZPing;

[HarmonyPatch(typeof(Chat), "RPC_ChatMessage")]
internal static class ZPingVanillaFallbackPatch
{
    private static bool Prefix(int type, string text) =>
        type != (int)Talker.Type.Ping || text != ZPingNetwork.VanillaPingMarker;
}