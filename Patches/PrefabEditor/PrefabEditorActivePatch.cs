using HarmonyLib;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain;

namespace JoksterCube.PingMark.Patches.PrefabEditor;

[HarmonyPatch(typeof(Menu), nameof(Menu.IsActive))]
internal static class PrefabEditorActivePatch
{
    private static void Postfix(ref bool __result) => __result |= PrefabCollectionEditor.BlocksInput;
}