using HarmonyLib;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain;

namespace JoksterCube.PingMark.Patches.PrefabEditor;

[HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
internal static class PrefabEditorMouseCapturePatch
{
    private static bool Prefix()
    {
        if (!PrefabCollectionEditor.IsOpen) return true;
        PrefabCollectionEditor.ReleaseMouse();
        return false;
    }
}