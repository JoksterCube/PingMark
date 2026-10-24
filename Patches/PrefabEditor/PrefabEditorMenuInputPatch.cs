using HarmonyLib;
using JoksterCube.PingMark.Domain;
using JoksterCube.PingMark.Domain.Collections;

namespace JoksterCube.PingMark.Patches.PrefabEditor;

[HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
internal static class PrefabEditorMenuInputPatch
{
    private static bool Prefix() => !PrefabCollectionEditor.BlocksInput;
}