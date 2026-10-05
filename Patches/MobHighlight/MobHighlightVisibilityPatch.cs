using HarmonyLib;
using MobHighlightService = JoksterCube.PingMark.Domain.MobHighlight;

namespace JoksterCube.PingMark.Patches.MobHighlight;

[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.TestShow))]
internal static class MobHighlightVisibilityPatch
{
    private static void Postfix(Character __0, ref bool __result)
    {
        if (MobHighlightService.IsMarked(__0)) __result = true;
    }
}