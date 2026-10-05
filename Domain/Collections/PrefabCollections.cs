using System;
using System.Collections.Generic;
using System.Linq;
using JoksterCube.PingMark.Domain.Models;

namespace JoksterCube.PingMark.Domain.Collections;

internal sealed class PrefabCollections
{
    private readonly Stack<(string Name, int Source)[]> _undo = new();
    private readonly List<HashSet<string>> _collections = [new(StringComparer.OrdinalIgnoreCase)];

    internal PrefabCollections(IEnumerable<string> available, string valuables, string locations, string useful)
        : this(available, [valuables, locations, useful])
    {
    }

    internal PrefabCollections(IEnumerable<string> available, IEnumerable<PrefabCollectionSection> sections)
        : this(available, sections.Select(section => section.Prefabs))
    {
    }

    private PrefabCollections(IEnumerable<string> available, IEnumerable<string> configuredSections)
    {
        string[] configured = configuredSections.ToArray();
        for (int index = 0; index < configured.Length; index++)
            _collections.Add(new(StringComparer.OrdinalIgnoreCase));

        HashSet<string> assigned = new(StringComparer.OrdinalIgnoreCase);
        for (int index = configured.Length - 1; index >= 0; index--)
            foreach (string name in Parse(configured[index]))
                if (assigned.Add(name))
                    _collections[index + 1].Add(name);

        foreach (string name in available)
            if (!string.IsNullOrWhiteSpace(name) && assigned.Add(name))
                _collections[0].Add(name);
    }

    internal string[] Names(int collection, string search) => _collections[collection]
        .Where(name => name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal int Count(int collection) => _collections[collection].Count;

    internal int CollectionCount => _collections.Count;

    internal bool CanUndo => _undo.Count > 0;

    internal void AddSection() => _collections.Add(new(StringComparer.OrdinalIgnoreCase));

    internal void RemoveSection(int collection)
    {
        if (_collections.Count <= 2 || collection <= 0 || collection >= _collections.Count)
            throw new ArgumentOutOfRangeException(nameof(collection));
        _collections[0].UnionWith(_collections[collection]);
        _collections.RemoveAt(collection);
        _undo.Clear();
    }

    internal void Move(IEnumerable<string> names, int destination)
    {
        if (destination < 0 || destination >= _collections.Count)
            throw new ArgumentOutOfRangeException(nameof(destination));

        List<(string Name, int Source)> changes = [];
        foreach (string name in names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
        {
            int source = _collections.FindIndex(collection => collection.Contains(name));
            if (source < 0 || source == destination) continue;
            string originalName = _collections[source].First(existing => StringComparer.OrdinalIgnoreCase.Equals(existing, name));
            changes.Add((originalName, source));
            _collections[source].Remove(originalName);
            _collections[destination].Add(originalName);
        }
        if (changes.Count > 0) _undo.Push(changes.ToArray());
    }

    internal bool Undo()
    {
        if (!CanUndo) return false;
        foreach ((string name, int source) in _undo.Pop())
        {
            foreach (HashSet<string> collection in _collections) collection.Remove(name);
            _collections[source].Add(name);
        }
        return true;
    }

    internal int AutoSort(Func<string, int> destination, ISet<int> sources)
    {
        List<(string Name, int Source)> changes = [];
        foreach (int source in sources.OrderBy(index => index))
            foreach (string name in _collections[source].ToArray())
            {
                int target = destination(name);
                if (target <= 0 || target >= _collections.Count || target == source) continue;
                _collections[source].Remove(name);
                _collections[target].Add(name);
                changes.Add((name, source));
            }
        if (changes.Count > 0) _undo.Push(changes.ToArray());
        return changes.Count;
    }

    internal string Serialize(int collection) => string.Join(", ", Names(collection, string.Empty));

    private static IEnumerable<string> Parse(string value) => value
        .Split([','], StringSplitOptions.RemoveEmptyEntries)
        .Select(name => name.Trim())
        .Where(name => name.Length > 0);
}