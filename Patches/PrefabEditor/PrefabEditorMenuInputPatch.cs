using HarmonyLib;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain;

namespace JoksterCube.PingMark.Patches.PrefabEditor;

[HarmonyPatch(typeof(Menu), "Update")]
internal static class PrefabEditorMenuInputPatch
{
    private static bool Prefix() => !PrefabCollectionEditor.BlocksInput;
}