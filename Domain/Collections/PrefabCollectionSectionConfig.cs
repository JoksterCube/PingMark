using System;
using System.Collections.Generic;
using System.Linq;
using JoksterCube.PingMark.Domain.Models;
using UnityEngine;
using YamlDotNet.Serialization;
using static JoksterCube.PingMark.Settings.Constants.Groups.PrefabCollections;
using TargetLists = JoksterCube.PingMark.Settings.Constants.Groups.PingTargets;

namespace JoksterCube.PingMark.Domain.Collections;

internal static class PrefabCollectionSectionConfig
{
    private static readonly ISerializer Serializer = new SerializerBuilder().DisableAliases().Build();
    private static readonly IDeserializer Deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();
    private static string _cachedValue = string.Empty;
    private static List<PrefabCollectionSection> _cachedSections = [];

    // Never writes the config: an unreadable value keeps the last good sections (or the defaults)
    // in memory, so a typo in the file cannot wipe the configured collections.
    internal static List<PrefabCollectionSection> Load()
    {
        string value = PrefabCollectionStore.Value;
        if (value == _cachedValue && _cachedSections.Count > 0) return _cachedSections;
        _cachedValue = value;

        string? error = null;
        List<PrefabCollectionSection>? sections = string.IsNullOrWhiteSpace(value) ? [] : Parse(value, out error);
        if (sections == null)
        {
            Plugin.ModLogger.LogError($"Unable to read configured prefab sections ({error}); keeping the previous sections. Fix the collections file to apply changes.");
            if (_cachedSections.Count == 0) _cachedSections = Parse(TargetLists.PrefabSections.DefaultValue) ?? [];
            return _cachedSections;
        }

        _cachedSections = sections.Count > 0 ? sections : Parse(TargetLists.PrefabSections.DefaultValue) ?? [];
        return _cachedSections;
    }

    internal static List<PrefabCollectionSection>? Parse(string value) => Parse(value, out _);

    // Returns null for unreadable YAML; the caller decides how to report the error.
    internal static List<PrefabCollectionSection>? Parse(string value, out string? error)
    {
        error = null;
        try
        {
            return CreateSections(Deserializer.Deserialize<SerializedSections>(value));
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return null;
        }
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
        // Remote edits are applied once the server accepts and returns them, so the cache is not updated here.
        PrefabCollectionStore.Submit(Serialize(sections));
    }
}