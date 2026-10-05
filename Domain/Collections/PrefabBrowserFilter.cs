using System.Collections.Generic;
using System;
using JoksterCube.PingMark.Domain.Enums;

namespace JoksterCube.PingMark.Domain.Collections;

internal sealed class PrefabBrowserFilter
{
    private readonly Dictionary<string, PrefabBrowserKind> _kinds = new(StringComparer.OrdinalIgnoreCase);

    internal void Register(string name, PrefabBrowserKind kind)
    {
        _kinds.TryGetValue(name, out PrefabBrowserKind existing);
        _kinds[name] = existing | kind;
    }

    internal void RegisterLocation(string name, bool hasInterior) => Register(name,
        PrefabBrowserKind.Location | (hasInterior ? PrefabBrowserKind.Dungeon : PrefabBrowserKind.None));

    internal PrefabBrowserKind Kind(string name) => _kinds.TryGetValue(name, out PrefabBrowserKind kind)
        ? kind : PrefabBrowserKind.None;

    internal bool Includes(string name, PrefabBrowserKind hiddenKinds, PrefabBrowserKind onlyKinds = PrefabBrowserKind.None)
    {
        _kinds.TryGetValue(name, out PrefabBrowserKind kind);
        if (kind == PrefabBrowserKind.None) kind = PrefabBrowserKind.Uncategorized;
        return (kind & hiddenKinds) == PrefabBrowserKind.None
            && (onlyKinds == PrefabBrowserKind.None || (kind & onlyKinds) != PrefabBrowserKind.None);
    }
}