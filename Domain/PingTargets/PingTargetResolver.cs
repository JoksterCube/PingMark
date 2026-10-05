using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain.Enums;
using JoksterCube.PingMark.Domain.Models;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain.PingTargets;

internal static class PingTargetResolver
{
    private static readonly Func<ZoneSystem, int, ZoneSystem.ZoneLocation?>? GetLocationByHash =
        AccessTools.Method(typeof(ZoneSystem), nameof(ZoneSystem.GetLocation), [typeof(int)]) is { } method
            ? AccessTools.MethodDelegate<Func<ZoneSystem, int, ZoneSystem.ZoneLocation?>>(method)
            : null;

    internal static PingTarget? Resolve(Collider? collider, Vector3 position)
    {
        if (!collider || IsTerrain(collider!)) return ResolveLocation(position);

        GameObject root = GetPrefabRoot(collider!);
        if (DropsEnabled.IsOn() && root.TryGetComponent(out ItemDrop _))
            return new PingTarget(PingCategory.Drop, GetObjectName(collider!), root);

        if (GetGravestone(collider) is { } tombStone)
            return GravestonesEnabled.IsOn()
                ? new PingTarget(PingCategory.Gravestone, "Gravestone", tombStone.gameObject)
                : null;

        if (collider!.GetComponentInParent<Character>() is { } character)
        {
            if (character.IsPlayer())
                return PlayersEnabled.IsOn()
                    ? new PingTarget(PingCategory.Player, GetObjectName(collider), character.gameObject)
                    : null;

            return OthersEnabled.IsOn()
                ? new PingTarget(PingCategory.Other, GetObjectName(collider), character.gameObject)
                : null;
        }

        bool isLocation = TryGetLocationPrefabName(root, out string locationId);
        string prefabName = isLocation ? locationId : GetObjectId(root);
        PrefabCollectionSection? section = FindSection(prefabName);
        if (section != null)
        {
            string displayName = TryGetLocationDisplayName(root, out string locationDisplayName)
                ? locationDisplayName : GetObjectName(collider);
            return new PingTarget(PingCategory.PrefabSection, displayName, root, section.Color);
        }

        PingTarget? locationTarget = ResolveLocation(position);
        if (locationTarget.HasValue) return locationTarget;

        if (OthersEnabled.IsOn())
            return new PingTarget(PingCategory.Other, GetObjectName(collider), root);

        return null;
    }

    internal static PingTarget? FindTarget(Vector3 position)
    {
        PingTarget? nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Collider collider in Physics.OverlapSphere(position, 0.1f,
                     Settings.Constants.LayerMasks.MobHighlight, QueryTriggerInteraction.Collide))
        {
            if (!IsPingCollider(collider)) continue;
            if (IsTerrain(collider)) continue;
            float distance = (collider.ClosestPoint(position) - position).sqrMagnitude;
            if (distance > nearestDistance) continue;
            PingTarget? target = Resolve(collider, position);
            if (!target.HasValue) continue;
            if (distance == nearestDistance && (target.Value.Category != PingCategory.Gravestone
                || nearest?.Category == PingCategory.Gravestone)) continue;
            nearestDistance = distance;
            nearest = target;
        }
        return nearest ?? ResolveLocation(position);
    }

    private static PingTarget? ResolveLocation(Vector3 position)
    {
        Location? location = Location.GetLocation(position);
        if (!location) return null;

        LocationProxy? proxy = location!.GetComponentInParent<LocationProxy>();
        GameObject root = proxy ? proxy!.gameObject : location.gameObject;
        if (!TryGetLocationPrefabName(root, out string locationId))
            locationId = Clean(global::Utils.GetPrefabName(location.gameObject));

        PrefabCollectionSection? section = FindSection(locationId);
        if (section == null) return null;

        if (!TryGetLocationDisplayName(root, out string displayName)
            && !TrySetLocalized(location.m_discoverLabel, out displayName))
            displayName = locationId;

        // Area-based match: the hit object itself is not listed, so nothing is outlined.
        return new PingTarget(PingCategory.PrefabSection, displayName, null, section.Color);
    }

    private static PrefabCollectionSection? FindSection(string prefabId)
    {
        foreach (PrefabCollectionSection section in PrefabCollectionSectionConfig.Load())
            if (section.Enabled && section.Contains(prefabId)) return section;
        return null;
    }

    internal static string GetDebugNames(RaycastHit hit)
    {
        if (!hit.collider) return string.Empty;
        GameObject root = GetPrefabRoot(hit.collider);
        List<string> names = [];

        void Add(string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) names.Add($"{label}: {Clean(value!)}");
        }

        string objectId = GetObjectId(root);
        Add(IsRegisteredPrefab(root) ? "Prefab ID" : "Object ID", objectId);
        Add("Object Name", GetObjectName(hit.collider));
        if (TryGetLocationPrefabName(root, out string locationName)) Add("Location ID", locationName);
        if (TryGetLocationDisplayName(root, out string locationDisplayName)) Add("Location Name", locationDisplayName);
        Location? area = Location.GetLocation(hit.point);
        if (area)
        {
            LocationProxy? proxy = area!.GetComponentInParent<LocationProxy>();
            string areaId = proxy && TryGetLocationPrefabName(proxy!.gameObject, out string proxyId)
                ? proxyId
                : global::Utils.GetPrefabName(area.gameObject);
            Add("Area Location ID", areaId);
            Add("Area Location Name", Localize(area.m_discoverLabel));
        }
        Add("Character", hit.collider.GetComponentInParent<Character>()?.GetHoverName());
        Add("Hover", hit.collider.GetComponentInParent<Hoverable>()?.GetHoverName());
        Piece? piece = hit.collider.GetComponentInParent<Piece>();
        if (piece != null) Add("Piece", $"{Localize(piece.m_name)} ({piece.m_name})");
        Add("Plant", hit.collider.GetComponentInParent<Plant>()?.GetHoverName());
        Add("Collider", hit.collider.name);
        Add("Collider Layer", LayerMask.LayerToName(hit.collider.gameObject.layer));
        Add("Collider Trigger", hit.collider.isTrigger.ToString());
        Room? room = hit.collider.GetComponentInParent<Room>();
        if (room) Add("Dungeon Room", room!.gameObject.name);
        if (hit.collider is MeshCollider meshCollider && meshCollider.sharedMesh)
            Add("Collision Mesh", meshCollider.sharedMesh.name);
        List<string> hierarchy = [];
        for (Transform current = hit.collider.transform; current; current = current.parent)
        {
            hierarchy.Add(current.gameObject.name);
            if (room && current == room!.transform) break;
        }
        Add("Hit Path", string.Join(" / ", hierarchy));

        return string.Join("\n", names);
    }

    private static bool TryGetLocationPrefabName(GameObject root, out string locationName)
    {
        locationName = string.Empty;
        if (!root.TryGetComponent(out LocationProxy _) || !ZoneSystem.instance || GetLocationByHash == null) return false;

        ZDO? zdo = root.GetComponent<ZNetView>()?.GetZDO();
        if (zdo == null) return false;

        ZoneSystem.ZoneLocation? location = GetLocationByHash(ZoneSystem.instance, zdo.GetInt(ZDOVars.s_location));
        if (location == null) return false;

        locationName = Clean(location.m_prefabName ?? location.m_name);
        return locationName.Length > 0;
    }

    private static bool TryGetLocationDisplayName(GameObject root, out string displayName)
    {
        displayName = string.Empty;
        if (!root.TryGetComponent(out LocationProxy _)) return false;

        Location? location = root.GetComponentInChildren<Location>(true);
        if (location != null && TrySetLocalized(location.m_discoverLabel, out displayName)) return true;

        // Dungeon entrances carry the location label on their teleport, e.g. "$location_forestcrypt".
        foreach (Teleport teleport in root.GetComponentsInChildren<Teleport>(true))
        {
            if (TrySetLocalized(teleport.m_enterText, out displayName)) return true;
        }

        return false;
    }

    private static bool TrySetLocalized(string? token, out string text)
    {
        text = string.IsNullOrWhiteSpace(token) ? string.Empty : Clean(Localize(token!));
        return text.Length > 0;
    }

    private static string GetObjectName(Collider collider)
    {
        GameObject root = GetPrefabRoot(collider);
        if (TryGetLocationDisplayName(root, out string locationName)) return locationName;
        Character? character = root.GetComponent<Character>();
        if (character != null)
        {
            string? charHover = character.GetHoverName();
            if (!string.IsNullOrWhiteSpace(charHover))
            {
                string name = Clean(Localize(charHover!));
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
        }

        Hoverable? hoverable = root.GetComponent<Hoverable>();
        if (hoverable != null)
        {
            string? hover = hoverable.GetHoverName();
            if (!string.IsNullOrWhiteSpace(hover))
            {
                string name = Clean(Localize(hover!));
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
        }

        Piece? piece = root.GetComponent<Piece>();
        if (piece != null && !string.IsNullOrWhiteSpace(piece.m_name))
        {
            string name = Clean(Localize(piece.m_name));
            if (!string.IsNullOrWhiteSpace(name)) return name;
        }

        Plant? plant = root.GetComponent<Plant>();
        if (plant != null)
        {
            string? plantHover = plant.GetHoverName();
            if (!string.IsNullOrWhiteSpace(plantHover))
            {
                string name = Clean(Localize(plantHover!));
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
        }

        return GetObjectId(root);
    }

    internal static TombStone? GetGravestone(Collider collider) =>
        GetColliderOwner(collider).GetComponentInParent<TombStone>();

    internal static bool IsPingCollider(Collider collider) => !collider.isTrigger
        || collider.GetComponentInParent<Pickable>() || collider.GetComponentInParent<PickableItem>();

    private static GameObject GetColliderOwner(Collider collider)
    {
        FloatingTerrainDummy? dummy = collider.GetComponentInParent<FloatingTerrainDummy>();
        return dummy && dummy!.m_parent ? dummy.m_parent.gameObject : collider.gameObject;
    }

    internal static GameObject GetPrefabRoot(Collider collider)
    {
        GameObject owner = GetColliderOwner(collider);
        GameObject? roomObject = null;
        for (Transform current = owner.transform; current; current = current.parent)
        {
            GameObject candidate = current.gameObject;
            if (candidate.GetComponent<Room>()) return roomObject ?? owner;
            if (candidate.GetComponent<ZNetView>() || candidate.GetComponent<Hoverable>() != null
                || candidate.GetComponent<Destructible>() || candidate.GetComponent<Piece>()
                || candidate.GetComponent<Plant>() || IsRegisteredPrefab(candidate))
                return candidate;
            if (roomObject == null && current != owner.transform) roomObject = candidate;
        }
        return owner.transform.root ? owner.transform.root.gameObject : owner;
    }

    private static bool IsRegisteredPrefab(GameObject candidate) =>
        ZNetScene.instance && ZNetScene.instance.GetPrefab(GetObjectId(candidate));

    internal static string GetObjectId(GameObject candidate) =>
        Regex.Replace(Clean(global::Utils.GetPrefabName(candidate)), @"\s+\(\d+\)$", string.Empty);

    private static bool IsTerrain(Collider collider) =>
        collider.gameObject.layer == LayerMask.NameToLayer("terrain")
        || collider.GetComponentInParent<Heightmap>() != null;

    private static string Localize(string text, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(text)) return fallback;
        if (Localization.instance != null)
        {
            string localized = Localization.instance.Localize(text);
            if (!string.IsNullOrWhiteSpace(localized)) return localized;
        }
        return text;
    }

    private static string Clean(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text.Trim().Replace('\r', ' ').Replace('\n', ' ');
    }
}
