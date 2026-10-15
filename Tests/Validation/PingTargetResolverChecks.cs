using System;
using System.Collections.Generic;
using System.Reflection;
using PingMarkResolverValidation.Domain;
using PingMarkResolverValidation.UnityEngine;

namespace PingMarkResolverValidation.UnityEngine
{
    public class Object
    {
        public string name = "";
        public static implicit operator bool(Object value) => value != null;
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() => gameObject.GetComponent<T>();
        public T GetComponentInParent<T>() => gameObject.GetComponentInParent<T>();
        public T GetComponentInChildren<T>(bool inactive) => gameObject.GetComponentInChildren<T>(inactive);
    }
    public class Transform : Component
    {
        public Transform parent;
        public Transform root => parent ? parent.root : this;
    }
    public class GameObject : Object
    {
        private readonly List<Component> parts = new();
        private readonly List<GameObject> children = new();
        public Transform transform;
        public int layer;
        public GameObject(string id, params Component[] components)
        {
            name = id;
            transform = new Transform();
            Add(transform);
            foreach (Component component in components) Add(component);
        }
        public void Add(Component component) { component.gameObject = this; parts.Add(component); }
        public GameObject Attach(GameObject child) { children.Add(child); child.transform.parent = transform; return child; }
        public T GetComponent<T>()
        {
            foreach (Component component in parts) if (component is T result) return result;
            return default;
        }
        public bool TryGetComponent<T>(out T result) { result = GetComponent<T>(); return result is not null; }
        public T GetComponentInParent<T>()
        {
            for (Transform current = transform; current; current = current.parent)
                if (current.gameObject.GetComponent<T>() is T result) return result;
            return default;
        }
        public T GetComponentInChildren<T>(bool inactive)
        {
            T[] results = GetComponentsInChildren<T>(inactive);
            return results.Length > 0 ? results[0] : default;
        }
        public T[] GetComponentsInChildren<T>(bool inactive)
        {
            List<T> results = new();
            foreach (Component component in parts) if (component is T result) results.Add(result);
            foreach (GameObject child in children) results.AddRange(child.GetComponentsInChildren<T>(inactive));
            return results.ToArray();
        }
    }
    public class Collider : Component { public bool isTrigger; public Vector3 ClosestPoint(Vector3 point) => point; }
    public class Mesh : Object { }
    public class MeshCollider : Collider { public Mesh sharedMesh; }
    public struct Vector3
    {
        public float sqrMagnitude => 0;
        public static Vector3 operator -(Vector3 left, Vector3 right) => default;
    }
    public struct Color { }
    public struct RaycastHit { public Collider collider; public Vector3 point; }
    public static class LayerMask
    {
        public static int NameToLayer(string name) => 99;
        public static string LayerToName(int layer) => layer == 98 ? "piece_nonsolid" : "Default";
    }
    public enum QueryTriggerInteraction { Ignore, Collide }
    public static class Physics
    {
        public static Collider[] Overlaps = [];
        public static QueryTriggerInteraction LastQuery;
        public static Collider[] OverlapSphere(Vector3 point, float radius, int mask, QueryTriggerInteraction query)
        {
            LastQuery = query;
            return query == QueryTriggerInteraction.Collide ? Overlaps : Array.FindAll(Overlaps, collider => !collider.isTrigger);
        }
    }
}
namespace PingMarkResolverValidation.BepInEx.Configuration
{
    public class ConfigEntry<T> { public T Value; }
}
namespace PingMarkResolverValidation.HarmonyLib
{
    public static class AccessTools
    {
        public static MethodInfo Method(Type type, string name, Type[] args) => null;
        public static T MethodDelegate<T>(MethodInfo method) => default;
    }
}
namespace PingMarkResolverValidation.Common
{
    public static class ToggleExtensions
    {
        public static bool IsOn(this BepInEx.Configuration.ConfigEntry<bool> config) => config.Value;
    }
}
namespace PingMarkResolverValidation.Settings
{
    public static class Constants { public static class LayerMasks { public const int MobHighlight = 0; } }
    public static class PluginConfig
    {
        public static BepInEx.Configuration.ConfigEntry<bool> DropsEnabled = new(), GravestonesEnabled = new(),
            PlayersEnabled = new(), OthersEnabled = new(), LocationsEnabled = new(), ValuablesResourcesEnabled = new(), OtherPrefabsEnabled = new();
        public static BepInEx.Configuration.ConfigEntry<string> LocationPrefabs = new(), ValuablesResourcesPrefabs = new(), OtherPrefabs = new();
        public static BepInEx.Configuration.ConfigEntry<Color> PingLocationColor = new(), PingValuablesResourcesColor = new(),
            PingOtherColor = new(), PingDropColor = new(), PingPlayerColor = new(), PingAnythingColor = new();
    }
}
namespace PingMarkResolverValidation
{
    public static class Utils
    {
        public static string GetPrefabName(GameObject value) => value.name.Replace("(Clone)", "");
    }
    public interface Hoverable { string GetHoverName(); }
    public class Pickable : Component, Hoverable { public string GetHoverName() => "$crystal"; }
    public class PickableItem : Component, Hoverable { public string GetHoverName() => "Crystal item"; }
    public class Destructible : Component { }
    public class ItemDrop : Component, Hoverable { public string GetHoverName() => "Crystal drop"; }
    public class TombStone : Component { }
    public class Character : Component, Hoverable
    {
        public bool IsPlayer() => false;
        public string GetHoverName() => "Creature";
    }
    public class Piece : Component { public string m_name = ""; }
    public class Plant : Component { public string GetHoverName() => "Plant"; }
    public class FloatingTerrain : Component { }
    public class FloatingTerrainDummy : Component { public FloatingTerrain m_parent; }
    public class Heightmap : Component { }
    public class LocationProxy : Component { }
    public class Room : Component { }
    public class Teleport : Component { public string m_enterText = ""; }
    public class ZNetView : Component { public ZDO GetZDO() => null; }
    public class ZDO { public int GetInt(int key) => 0; }
    public static class ZDOVars { public const int s_location = 0; }
    public class ZoneSystem : UnityEngine.Object
    {
        public static ZoneSystem instance;
        public ZoneLocation GetLocation(int hash) => null;
        public class ZoneLocation { public string m_prefabName, m_name; }
    }
    public class Location : Component
    {
        public string m_discoverLabel = "";
        public static Location Area;
        public static Location GetLocation(Vector3 position) => Area;
    }
    public class Localization
    {
        public static Localization instance = new();
        public string Localize(string text) => text == "$crystal" ? "Crystal" : text;
    }
    public class ZNetScene : UnityEngine.Object
    {
        public static ZNetScene instance;
        public readonly Dictionary<string, GameObject> Prefabs = new();
        public GameObject GetPrefab(string name) => Prefabs.TryGetValue(name, out GameObject prefab) ? prefab : null;
    }
    public static class Checks
    {
        private static int count;
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            count++;
        }
        private static GameObject Root(Collider collider) => (GameObject)typeof(PingTargetResolver)
            .GetMethod("GetPrefabRoot", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, [collider]);
        public static string Run()
        {
            count = 0;
            ZNetScene.instance = new();
            Physics.Overlaps = [];
            Location.Area = null;
            Settings.PluginConfig.LocationsEnabled.Value = false;
            Settings.PluginConfig.ValuablesResourcesEnabled.Value = true;
            Settings.PluginConfig.ValuablesResourcesPrefabs.Value = "Pickable_Crystal, ice_crystal";
            Settings.PluginConfig.OthersEnabled.Value = true;
            GameObject cave = new("DG_Cave", new ZNetView());
            GameObject crystal = cave.Attach(new("Pickable_Crystal(Clone)", new Pickable()));
            Collider hit = crystal.Attach(new("mesh", new Collider())).GetComponent<Collider>();
            Check(Root(hit) == crystal, "Pickable wins over cave ZNetView");
            PingTarget target = PingTargetResolver.Resolve(hit, default).Value;
            Check(target.Category == PingCategory.ValuablesResources && target.Name == "Crystal", "Crystal category and localized name");
            Check(target.OutlineTargets.Length == 1 && target.OutlineTargets[0] == crystal, "Only crystal outlined");
            GameObject ice = cave.Attach(new("ice_crystal", new Destructible()));
            Collider iceHit = ice.Attach(new("collision", new Collider())).GetComponent<Collider>();
            Check(Root(iceHit) == ice, "Destructible wins over cave ZNetView");
            Check(PingTargetResolver.Resolve(iceHit, default).Value.Name == "ice crystal", "Unnamed destructible gets object-name fallback");
            GameObject staticProp = cave.Attach(new("cave_prop(Clone)"));
            Collider propHit = staticProp.Attach(new("mesh", new Collider())).GetComponent<Collider>();
            ZNetScene.instance.Prefabs["cave_prop"] = new("cave_prop");
            Check(Root(propHit) == staticProp, "Registered non-networked prefab wins over cave");
            Check(PingTargetResolver.GetDebugNames(new() { collider = propHit }).Contains("Prefab ID: cave_prop"), "Registered prefab gets prefab ID");
            string debug = PingTargetResolver.GetDebugNames(new() { collider = iceHit });
            Check(debug.Contains("Object ID: ice_crystal") && debug.Contains("Object Name: ice_crystal"), "Embedded object distinguished from registered prefab");
            GameObject parent = new("hover_parent", new Pickable());
            parent.Attach(staticProp);
            Check(PingTargetResolver.Resolve(propHit, default).Value.Name == "cave prop", "Name does not leak from parent hoverable");
            Collider wallHit = cave.Attach(new("wall", new Collider())).GetComponent<Collider>();
            Check(Root(wallHit) == cave, "Unidentified cave geometry retains network root fallback");
            GameObject room = cave.Attach(new("cave_room(Clone)", new Room()));
            GameObject geometry = room.Attach(new("ice_geometry"));
            Collider geometryHit = geometry.Attach(new("default", new MeshCollider
                { sharedMesh = new Mesh { name = "ice_mesh" } })).GetComponent<Collider>();
            Check(Root(geometryHit) == geometry, "Componentless room geometry stops before dungeon container");
            string geometryDebug = PingTargetResolver.GetDebugNames(new() { collider = geometryHit });
            Check(geometryDebug.Contains("Dungeon Room: cave_room(Clone)")
                && geometryDebug.Contains("Collision Mesh: ice_mesh"), "Room and collision mesh exposed for embedded geometry");
            Check(geometryDebug.Contains("Hit Path: default / ice_geometry / cave_room(Clone)")
                && !geometryDebug.Contains("cave_room(Clone) / DG_Cave"), "Hit path includes parents and stops at dungeon room");
            GameObject iceRoom = cave.Attach(new("cave_new_icecorridor07", new Room()));
            GameObject crystals = iceRoom.Attach(new("crystals"));
            GameObject rock = crystals.Attach(new("caverock_rockbun (1)"));
            Collider rockHit = rock.Attach(new("default", new MeshCollider
                { sharedMesh = new Mesh { name = "default" } })).GetComponent<Collider>();
            Check(Root(rockHit) == rock, "Actual crystal hierarchy resolves individual rock, not crystals group or DG_Cave");
            string rockDebug = PingTargetResolver.GetDebugNames(new() { collider = rockHit });
            Check(rockDebug.Contains("Object ID: caverock_rockbun") && !rockDebug.Contains("caverock_rockbun (1)\n"),
                "Embedded object ID excludes duplicate suffix without claiming a registered prefab");
            GameObject area = new("MountainCave02", new Location { m_discoverLabel = "Frost Caves" });
            Location.Area = area.GetComponent<Location>();
            Settings.PluginConfig.LocationsEnabled.Value = true;
            Settings.PluginConfig.LocationPrefabs.Value = "MountainCave02";
            PingTarget rockTarget = PingTargetResolver.Resolve(rockHit, default).Value;
            Check(rockTarget.Category == PingCategory.Location && rockTarget.Name == "Frost Caves",
                "Matching cave area takes priority over all-other room fallback");
            Check(rockTarget.OutlineTargets.Length == 0,
                "Area match does not outline unlisted room geometry");
            Settings.PluginConfig.OthersEnabled.Value = false;
            Check(PingTargetResolver.Resolve(rockHit, default).Value.Category == PingCategory.Location,
                "Disabling Others preserves cave area fallback");
            Settings.PluginConfig.ValuablesResourcesPrefabs.Value = "Pickable_Crystal, ice_crystal, caverock_rockbun";
            Check(PingTargetResolver.Resolve(rockHit, default).Value.Category == PingCategory.ValuablesResources,
                "Stable embedded object ID can be configured as resource");
            Settings.PluginConfig.LocationsEnabled.Value = false;
            Settings.PluginConfig.OthersEnabled.Value = true;
            Settings.PluginConfig.ValuablesResourcesPrefabs.Value = "MountainCave02";
            PingTarget resourceAreaTarget = PingTargetResolver.Resolve(rockHit, default).Value;
            Check(resourceAreaTarget.Category == PingCategory.ValuablesResources && resourceAreaTarget.Name == "Frost Caves",
                "Area ID in resources list classifies unlisted object as resource");
            Settings.PluginConfig.ValuablesResourcesEnabled.Value = false;
            Settings.PluginConfig.OtherPrefabsEnabled.Value = true;
            Settings.PluginConfig.OtherPrefabs.Value = "MountainCave02";
            Check(PingTargetResolver.Resolve(rockHit, default).Value.Category == PingCategory.OtherPrefab,
                "Area ID in other prefabs list classifies unlisted object as other prefab");
            Settings.PluginConfig.ValuablesResourcesEnabled.Value = true;
            Settings.PluginConfig.ValuablesResourcesPrefabs.Value = "MountainCave02";
            Settings.PluginConfig.OtherPrefabs.Value = "caverock_rockbun";
            Check(PingTargetResolver.Resolve(rockHit, default).Value.Category == PingCategory.OtherPrefab,
                "Exact object ID takes priority over matching resource area ID");
            Settings.PluginConfig.OthersEnabled.Value = true;
            Collider directHit = iceRoom.Attach(new("floor", new Collider())).GetComponent<Collider>();
            Check(Root(directHit) == directHit.gameObject, "Direct room child does not outline entire room");
            GameObject roomPickable = iceRoom.Attach(new("Pickable_Crystal(Clone)", new Pickable()));
            Collider roomPickableHit = roomPickable.Attach(new("mesh", new Collider())).GetComponent<Collider>();
            Check(Root(roomPickableHit) == roomPickable
                && PingTargetResolver.Resolve(roomPickableHit, default).Value.Name == "Crystal",
                "Real gameplay owner inside room retains priority and localized name");
            Location.Area = null;
            Settings.PluginConfig.LocationsEnabled.Value = false;
            GameObject world = new("world");
            GameObject networked = world.Attach(new("Chest", new ZNetView()));
            Collider networkHit = networked.Attach(new("mesh", new Collider())).GetComponent<Collider>();
            Check(Root(networkHit) == networked, "Ordinary networked owner preserved");
            GameObject stone = new("TombStone", new TombStone(), new FloatingTerrain(), new ZNetView());
            Collider dummyHit = new GameObject("New", new Collider(), new FloatingTerrainDummy
                { m_parent = stone.GetComponent<FloatingTerrain>() }).GetComponent<Collider>();
            Check(Root(dummyHit) == stone && PingTargetResolver.GetGravestone(dummyHit) == stone.GetComponent<TombStone>(), "Floating gravestone remapping preserved");
            ZNetScene.instance = null;
            Check(Root(iceHit) == ice, "Gameplay owner works without prefab registry");
            Collider anonymousHit = world.Attach(new("unknown", new Collider())).GetComponent<Collider>();
            Check(Root(anonymousHit) == world, "Unknown hierarchy retains root fallback");
            GameObject triggerCrystal = new("Pickable_Crystal", new Pickable());
            Collider triggerHit = triggerCrystal.Attach(new("interaction", new Collider { isTrigger = true })).GetComponent<Collider>();
            triggerHit.gameObject.layer = 98;
            Check(PingTargetResolver.IsPingCollider(triggerHit), "Pickable trigger is eligible for camera ping");
            Collider volumeHit = new GameObject("dungeon_volume", new Collider { isTrigger = true }).GetComponent<Collider>();
            Check(!PingTargetResolver.IsPingCollider(volumeHit), "Unrelated trigger is not eligible for camera ping");
            Collider itemHit = new GameObject("PickableItem", new PickableItem(), new Collider { isTrigger = true }).GetComponent<Collider>();
            Check(PingTargetResolver.IsPingCollider(itemHit), "PickableItem trigger is eligible for camera ping");
            Check(PingTargetResolver.IsPingCollider(wallHit), "Solid geometry remains eligible and blocks objects behind walls");
            Physics.Overlaps = [volumeHit, triggerHit];
            PingTarget triggerTarget = PingTargetResolver.FindTarget(default).Value;
            Check(Physics.LastQuery == QueryTriggerInteraction.Collide && triggerTarget.Name == "Crystal"
                && triggerTarget.Category == PingCategory.ValuablesResources, "Point lookup includes pickable triggers and skips unrelated volumes");
            Check(triggerTarget.OutlineTargets.Length == 1 && triggerTarget.OutlineTargets[0] == triggerCrystal,
                "Trigger collider resolves and outlines the pickable instead of dungeon decoration");
            string triggerDebug = PingTargetResolver.GetDebugNames(new() { collider = triggerHit });
            Check(triggerDebug.Contains("Collider Layer: piece_nonsolid") && triggerDebug.Contains("Collider Trigger: True"),
                "Diagnostics expose actual collider layer and trigger state");
            Physics.Overlaps = [];
            return count + " resolver checks passed";
        }
    }
}