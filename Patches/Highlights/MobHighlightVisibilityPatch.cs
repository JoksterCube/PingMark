using HarmonyLib;
using JoksterCube.PingMark.Domain;

namespace JoksterCube.PingMark.Patches.Highlights;

[HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.TestShow))]
internal static class MobHighlightVisibilityPatch
{
    private static void Postfix(Character __0, ref bool __result)
    {
        if (MobHighlight.IsMarked(__0)) __result = true;
    }
}