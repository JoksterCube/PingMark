using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static JoksterCube.PingMark.Settings.Constants.Groups.PrefabCollections;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain.Models;

internal sealed class PrefabCollectionSection
{
    private string? _cachedPrefabs;
    private HashSet<string>? _prefabs;

    internal string Name { get; set; } = string.Empty;
    internal string Prefabs { get; set; } = string.Empty;
    internal bool Enabled { get; set; } = true;
    internal int ColorId { get; set; } = Color1Id;
    internal Color Color => GetPrefabSectionColor(ColorId);

    internal PrefabCollectionSection Copy() => new()
    {
        Name = Name,
        Prefabs = Prefabs,
        Enabled = Enabled,
        ColorId = ColorId
    };

    internal bool Contains(string prefab)
    {
        if (_cachedPrefabs != Prefabs)
        {
            _cachedPrefabs = Prefabs;
            _prefabs = new(Prefabs.Split(',')
                .Select(value => value.Trim())
                .Where(value => value.Length > 0), StringComparer.OrdinalIgnoreCase);
        }
        return _prefabs!.Contains(prefab);
    }
}