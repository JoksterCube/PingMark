using UnityEngine;

namespace JoksterCube.PingMark.Domain.Collections;

// Enabled only while the editor is open, so Unity does not run IMGUI for the mod every frame otherwise.
internal sealed class PrefabCollectionEditorView : MonoBehaviour
{
    private Plugin? _plugin;

    internal static PrefabCollectionEditorView Create(Plugin plugin)
    {
        PrefabCollectionEditorView view = plugin.gameObject.AddComponent<PrefabCollectionEditorView>();
        view._plugin = plugin;
        view.useGUILayout = false;
        view.enabled = false;
        return view;
    }

    private void OnGUI()
    {
        if (_plugin) PrefabCollectionEditor.Draw(_plugin!);
    }
}
