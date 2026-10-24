using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain.Enums;
using JoksterCube.PingMark.Domain.Models;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain.PingTargets;

internal static class PingTargetResolver
{
    private static readonly Regex InstanceSuffix = new(@"\s+\(\d+\)$", RegexOptions.Compiled);


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

        // Walls and floors of generated rooms are not objects, so they never match a section by their group name
        // (older defaults list "Floor", "Walls", ...). Outdoors they still fall back to the location's name.
        // Geometry of a fixed (non-generated) interior resolves to its location and is skipped the same way.
        bool isLocation = TryGetLocationPrefabName(root, out string locationId);
        if (!IsRoomGeometry(root) && !(isLocation && Character.InInterior(position)))
        {
            string prefabName = isLocation ? locationId : GetObjectId(root);
            PrefabCollectionSection? section = FindSection(prefabName);
            if (section != null)
            {
                string displayName = TryGetLocationDisplayName(root, out string locationDisplayName)
                    ? locationDisplayName : GetObjectName(collider);
                // A hit on a location's own geometry (a troll cave wall, a ruin) names the location like an area
                // ping instead of outlining an arbitrary part of the whole site.
                return new PingTarget(PingCategory.PrefabSection, displayName, isLocation ? null : root, section.Color);
            }
        }

        PingTarget? locationTarget = ResolveLocation(position);
        if (locationTarget.HasValue) return locationTarget;

        if (OthersEnabled.IsOn())
            return new PingTarget(PingCategory.Other, GetObjectName(collider), root);

        return null;
    }

    // The networked object the hit belongs to, so other players can resolve exactly the same object.
    internal static ZDOID GetNetworkId(Collider collider)
    {
        GameObject root = GetPrefabRoot(collider);
        return root.TryGetComponent(out ZNetView view) && view.GetZDO() is { } zdo ? zdo.m_uid : ZDOID.None;
    }

    // Resolves a target from its network ID; null when the object is not loaded here.
    internal static PingTarget? ResolveInstance(ZDOID id, Vector3 position)
    {
        if (id.IsNone() || !ZNetScene.instance) return null;
        GameObject instance = ZNetScene.instance.FindInstance(id);
        if (!instance) return null;
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            if (IsPingCollider(collider) && GetPrefabRoot(collider) == instance) return Resolve(collider, position);
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
            float distance = (ClosestPoint(collider, position) - position).sqrMagnitude;
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

    // Physics.ClosestPoint only supports primitives and convex meshes; rocks and cliffs use concave mesh colliders.
    private static Vector3 ClosestPoint(Collider collider, Vector3 position) =>
        collider is MeshCollider { convex: false } ? collider.bounds.ClosestPoint(position) : collider.ClosestPoint(position);

    private static PingTarget? ResolveLocation(Vector3 position)
    {
        // Teleport interiors (crypts, caves, Dvergr and Deep North dungeons) are one location throughout,
        // so naming every spot inside after it would make the whole dungeon pingable.
        if (Character.InInterior(position)) return null;

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

    // Matches the editor, where a prefab listed in several sections belongs to the last one; when that
    // section is disabled the prefab is not pinged as a section member at all.
    private static PrefabCollectionSection? FindSection(string prefabId)
    {
        List<PrefabCollectionSection> sections = PrefabCollectionSectionConfig.Load();
        for (int index = sections.Count - 1; index >= 0; index--)
            if (sections[index].Contains(prefabId)) return sections[index].Enabled ? sections[index] : null;
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
        if (!root.TryGetComponent(out LocationProxy _) || !ZoneSystem.instance) return false;

        ZDO? zdo = root.GetComponent<ZNetView>()?.GetZDO();
        if (zdo == null) return false;

        ZoneSystem.ZoneLocation? location = ZoneSystem.instance.GetLocation(zdo.GetInt(ZDOVars.s_location));
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
        || collider.GetComponentInParent<Pickable>() || collider.GetComponentInParent<PickableItem>()
        || collider.GetComponentInParent<RandomFlyingBird>();

    private static GameObject GetColliderOwner(Collider collider)
    {
        FloatingTerrainDummy? dummy = collider.GetComponentInParent<FloatingTerrainDummy>();
        return dummy && dummy!.m_parent ? dummy.m_parent.gameObject : collider.gameObject;
    }

    internal static GameObject GetPrefabRoot(Collider collider)
    {
        GameObject owner = GetColliderOwner(collider);
        GameObject? roomObject = null;
        // A name match alone is weak evidence: MineRock_Tin's model part "Rock_4 (1)" shares its name with the
        // standalone Rock_4 prefab. Remember it, but let a networked/interactive ancestor claim the hit first.
        GameObject? namedPrefab = null;
        for (Transform current = owner.transform; current; current = current.parent)
        {
            GameObject candidate = current.gameObject;
            if (candidate.GetComponent<Room>()) return namedPrefab ?? roomObject ?? owner;
            // Location proxies are networked too, but their non-networked props are still meant to resolve by name.
            if (candidate.GetComponent<ZNetView>())
                return namedPrefab && candidate.GetComponent<LocationProxy>() ? namedPrefab! : candidate;
            if (IsObject(candidate)) return candidate;
            if (namedPrefab == null && IsRegisteredPrefab(candidate)) namedPrefab = candidate;
            if (roomObject == null && current != owner.transform) roomObject = candidate;
        }
        if (namedPrefab) return namedPrefab!;
        return owner.transform.root ? owner.transform.root.gameObject : owner;
    }

    // GetPrefabRoot falls back to a room child such as "floor" when the hit belongs to no object at all. Rooms
    // build interiors and also outdoor camps (Fuling villages, the Ashlands fortress).
    private static bool IsRoomGeometry(GameObject root) => root.GetComponentInParent<Room>()
        && !root.GetComponent<ZNetView>() && !IsObject(root) && !IsRegisteredPrefab(root);

    // Interfaces rather than concrete types, so modded objects (custom destructibles, interactables, hover
    // targets) count as objects even when they do not derive from the vanilla classes.
    private static bool IsObject(GameObject candidate) =>
        candidate.GetComponent<Hoverable>() != null || candidate.GetComponent<Interactable>() != null
        || candidate.GetComponent<IDestructible>() != null || candidate.GetComponent<Piece>()
        || candidate.GetComponent<Plant>();

    private static bool IsRegisteredPrefab(GameObject candidate) =>
        ZNetScene.instance && ZNetScene.instance.GetPrefab(GetObjectId(candidate));

    internal static string GetObjectId(GameObject candidate) =>
        InstanceSuffix.Replace(Clean(global::Utils.GetPrefabName(candidate)), string.Empty);

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
