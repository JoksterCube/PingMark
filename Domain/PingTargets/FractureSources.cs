using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.PingMark.Domain.PingTargets;

// Maps the pieces an object breaks into ("mudpile2_frac", "rock4_copper_frac") back to the objects that spawn
// them, so a fractured deposit keeps pinging like the intact one when only the intact one is listed.
internal static class FractureSources
{
    private static readonly Dictionary<string, List<string>> Sources = new(StringComparer.OrdinalIgnoreCase);
    private static ZNetScene? _scene;

    internal static IReadOnlyList<string> SourcesOf(string prefabId)
    {
        if (!ZNetScene.instance) return [];
        if (_scene != ZNetScene.instance) Build(ZNetScene.instance);
        return Sources.TryGetValue(prefabId, out List<string> sources) ? sources : [];
    }

    private static void Build(ZNetScene scene)
    {
        _scene = scene;
        Sources.Clear();
        foreach (GameObject prefab in scene.m_prefabs)
        {
            if (!prefab || !prefab.TryGetComponent(out Destructible destructible) || !destructible.m_spawnWhenDestroyed) continue;
            string spawned = global::Utils.GetPrefabName(destructible.m_spawnWhenDestroyed);
            // Only replacements that are still something to mine, not debris effects.
            if (!destructible.m_spawnWhenDestroyed.GetComponent<MineRock5>() && !destructible.m_spawnWhenDestroyed.GetComponent<MineRock>())
                continue;
            if (!Sources.TryGetValue(spawned, out List<string> sources)) Sources[spawned] = sources = [];
            sources.Add(prefab.name);
        }
    }
}
