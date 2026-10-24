using HarmonyLib;
using JoksterCube.PingMark.Domain;
using JoksterCube.PingMark.Domain.Collections;

namespace JoksterCube.PingMark.Patches.PrefabEditor;

[HarmonyPatch(typeof(Menu), nameof(Menu.IsVisible))]
internal static class PrefabEditorVisiblePatch
{
    private static void Postfix(ref bool __result) => __result |= PrefabCollectionEditor.BlocksInput;
}