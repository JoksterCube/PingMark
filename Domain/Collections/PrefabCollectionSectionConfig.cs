using System.Collections.Generic;
using System.Linq;
using JoksterCube.PingMark.Domain.Models;
using UnityEngine;
using YamlDotNet.Serialization;
using static JoksterCube.PingMark.Settings.PluginConfig;
using static JoksterCube.PingMark.Settings.Constants.Groups.PrefabCollections;
using TargetLists = JoksterCube.PingMark.Settings.Constants.Groups.PingTargets;

namespace JoksterCube.PingMark.Domain.Collections;

internal static class PrefabCollectionSectionConfig
{
    private static readonly ISerializer Serializer = new SerializerBuilder().DisableAliases().Build();
    private static readonly IDeserializer Deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
    private static string _cachedValue = string.Empty;
    private static List<PrefabCollectionSection> _cachedSections = [];

    internal static List<PrefabCollectionSection> Load()
    {
        string value = PrefabSections.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            value = TargetLists.PrefabSections.DefaultValue;
            PrefabSections.Value = value;
        }
        if (value == _cachedValue && _cachedSections.Count > 0) return _cachedSections;

        SerializedSections? serialized = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(value)) serialized = Deserializer.Deserialize<SerializedSections>(value);
        }
        catch
        {
            Plugin.ModLogger.LogError("Unable to read configured prefab sections; restoring the configured section layout.");
            value = TargetLists.PrefabSections.DefaultValue;
            PrefabSections.Value = value;
            serialized = Deserializer.Deserialize<SerializedSections>(value);
        }

        _cachedSections = CreateSections(serialized);

        if (_cachedSections.Count == 0)
        {
            value = TargetLists.PrefabSections.DefaultValue;
            PrefabSections.Value = value;
            serialized = Deserializer.Deserialize<SerializedSections>(value);
            _cachedSections = CreateSections(serialized);
        }
        _cachedValue = value;
        return _cachedSections;
    }

    private static List<PrefabCollectionSection> CreateSections(SerializedSections? serialized) => serialized?.Sections?
        .Select(section => new PrefabCollectionSection
        {
            Name = section.Name ?? string.Empty,
            Prefabs = section.Prefabs ?? string.Empty,
            Enabled = section.Enabled,
            ColorId = Mathf.Clamp(section.ColorId, Color1Id, Colors.Length)
        })
        .Where(section => !string.IsNullOrWhiteSpace(section.Name))
        .ToList() ?? [];

    internal static List<PrefabCollectionSection> LoadCopies() => Load().Select(section => section.Copy()).ToList();

    internal static string Serialize(IEnumerable<PrefabCollectionSection> sections) => Serializer.Serialize(new SerializedSections
    {
        Sections = sections.Select(section => new SerializedSection
        {
            Name = section.Name.Trim(),
            Prefabs = section.Prefabs,
            Enabled = section.Enabled,
            ColorId = section.ColorId
        }).ToList()
    });

    internal static void Save(List<PrefabCollectionSection> sections)
    {
        string value = Serialize(sections);
        PrefabSections.Value = value;
        _cachedValue = value;
        _cachedSections = sections.Select(section => section.Copy()).ToList();
    }
}