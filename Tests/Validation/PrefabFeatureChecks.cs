namespace UnityEngine
{
    public enum FindObjectsSortMode { None }
    public class Object
    {
        public string name = "";
        public static readonly System.Collections.Generic.List<Object> SceneObjects = new();
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) => System.Linq.Enumerable.ToArray(System.Linq.Enumera.OfType<T>(SceneObjects));
        public static implicit operator bool(Object value) => value != null;
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() => gameObject.GetComponent<T>();
        public T GetComponentInParent<T>()
        {
            for (Transform current = transform; current != null; current = current.parent)
                if (current.GetComponent<T>() is T result) return result;
            return default;
        }
    }
    public class Transform : Component
    {
        public Transform parent;
        public bool IsChildOf(Transform other) { for (var current = this; current != null; current = current.parent) if (current == other) return true; return false; }
    }
    public class GameObject : Object
    {
        private readonly System.Collections.Generic.List<Component> parts = new();
        private readonly System.Collections.Generic.List<GameObject> children = new();
        public Transform transform;
        public GameObject(string id, params Component[] components)
        {
            name = id; transform = new Transform(); Add(transform);
            foreach (var component in components) Add(component);
        }
        public void Add(Component component) { component.gameObject = this; parts.Add(component); }
        public void Attach(GameObject child) { children.Add(child); child.transform.parent = transform; }
        public T GetComponent<T>() => System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OfType<T>(parts));
        public T GetComponentInChildren<T>(bool inactive) => System.Linq.Enumerable.FirstOrDefault(GetComponentsInChildren<T>(inactive));
        public T[] GetComponentsInChildren<T>(bool inactive)
        {
            var result = new System.Collections.Generic.List<T>(System.Linq.Enumerable.OfType<T>(parts));
            foreach (var child in children) result.AddRange(child.GetComponentsInChildren<T>(inactive));
            return result.ToArray();
        }
    }
    public class Renderer : Component { }
    public class ParticleSystem : Component { }
    public class AudioSource : Component { }
    public class Light : Component { }
    public class Collider : Component { public GameObject Target; }
    public class MeshCollider : Collider { }
    public class LODGroup : Component { }
}
namespace HarmonyLib
{
    public static class AccessTools
    {
        public static System.Reflection.FieldInfo Field(System.Type type, string name) => type.GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
    }
}
namespace SoftReferenceableAssets
{
    public struct SoftReference<T> where T : UnityEngine.Object
    {
        private T asset;
        public bool IsLoaded;
        public SoftReference(T value, bool loaded) { asset = value; IsLoaded = loaded; }
        public T Asset => IsLoaded ? asset : throw new System.InvalidOperationException("Unloaded asset accessed");
    }
}
public static class Utils { public static string GetPrefabName(UnityEngine.GameObject prefab) => prefab.name.Replace("(Clone)", ""); }
public class ZNetView : UnityEngine.Component { }
public class ZNetScene : UnityEngine.Object
{
    public static ZNetScene instance;
    public System.Collections.Generic.List<UnityEngine.GameObject> m_prefabs = new();
    public System.Collections.Generic.List<UnityEngine.GameObject> m_nonNetViewPrefabs = new();
}
public class ObjectDB : UnityEngine.Object
{
    public static ObjectDB instance;
    public System.Collections.Generic.List<UnityEngine.GameObject> m_items = new();
    public System.Collections.Generic.List<Recipe> m_recipes = new();
}
public class ItemDrop : UnityEngine.Component
{
    public ItemData m_itemData = new();
    public class ItemData
    {
        public SharedData m_shared = new();
        public enum ItemType { Material, Equipment, Consumable }
        public class SharedData { public PieceTable m_buildPieces; public ItemType m_itemType; }
    }
}
public class PieceTable : UnityEngine.Object { public System.Collections.Generic.List<UnityEngine.GameObject> m_pieces = new(); }
public class Piece : UnityEngine.Component
{
    public bool m_enabled = true;
    public int m_comfort;
    public PieceCategory m_category;
    public Requirement[] m_resources = new Requirement[0];
    public enum PieceCategory { Misc, Furniture }
    public class Requirement { public ItemDrop m_resItem; public int m_amount; }
}
public class Recipe : UnityEngine.Object
{
    public ItemDrop m_item;
    public Piece.Requirement[] m_resources = new Piece.Requirement[0];
    public bool m_enabled = true;
}
public class ItemConversion { public ItemDrop m_from, m_to; }
public class IncineratorConversion { public Piece.Requirement[] m_requirements = new Piece.Requirement[0]; }
public class ZoneSystem : UnityEngine.Object
{
    public static ZoneSystem instance;
    public System.Collections.Generic.List<ZoneVegetation> m_vegetation = new();
    public System.Collections.Generic.List<ZoneLocation> m_locations = new();
    public UnityEngine.GameObject m_zonePrefab, m_zoneCtrlPrefab, m_locationProxyPrefab;
    public class ZoneVegetation { public UnityEngine.GameObject m_prefab; }
    public class ZoneLocation
    {
        public string m_name, m_prefabName;
        public float m_interiorRadius;
        public SoftReferenceableAssets.SoftReference<UnityEngine.GameObject> m_prefab;
    }
}
public class DungeonDB : UnityEngine.Object
{
    public static DungeonDB instance;
    private object[] m_rooms;
    public DungeonDB(params object[] rooms) { m_rooms = rooms; }
    public class RoomData { public SoftReferenceableAssets.SoftReference<Room> prefab; }
}
public class Location : UnityEngine.Component
{
    public bool m_hasInterior, m_applyRandomDamage;
    public UnityEngine.GameObject m_interiorPrefab;
    public DungeonGenerator m_generator;
}
public class DungeonGenerator : UnityEngine.Component
{
    public enum Algorithm { Dungeon, CampGrid, CampRadial }
    public Algorithm m_algorithm;
    public Room.Theme m_themes;
}
public class Room : UnityEngine.Component
{
    [System.Flags] public enum Theme { Crypt = 1, Camp = 2, Modded = 4 }
    public Theme m_theme;
}
public class Plant : UnityEngine.Component { public UnityEngine.GameObject[] m_grownPrefabs; }
[System.Flags] public enum DestructibleType { None = 0, Default = 1, Tree = 2 }
public class Destructible : UnityEngine.Component
{
    public DestructibleType m_destructibleType;
    public int m_minToolTier;
    public UnityEngine.GameObject m_spawnWhenDamaged, m_spawnWhenDestroyed;
}
public class WearNTear : UnityEngine.Component
{
    public bool m_supports;
    public UnityEngine.GameObject m_worn, m_broken;
    public UnityEngine.GameObject[] m_fragmentRoots;
}
public class TreeBase : UnityEngine.Component { public UnityEngine.GameObject m_stubPrefab, m_logPrefab, m_spawnOnDamage; }
public class TreeLog : UnityEngine.Component { public UnityEngine.GameObject m_subLogPrefab; }
public class MineRock : UnityEngine.Component { public DropTable m_dropItems; }
public class MineRock5 : UnityEngine.Component { public DropTable m_dropItems; }
public class DropTable
{
    public System.Collections.Generic.List<DropData> m_drops = new();
    public class DropData { public UnityEngine.GameObject m_item; }
}
public class DropOnDestroyed : UnityEngine.Component { public DropTable m_dropWhenDestroyed; }
public class CreatureSpawner : UnityEngine.Component { public UnityEngine.GameObject m_creaturePrefab; }
public class TriggerSpawner : UnityEngine.Component { public UnityEngine.GameObject[] m_creaturePrefabs; }
public class EggGrow : UnityEngine.Component { public UnityEngine.GameObject m_grownPrefab; }
public class Container : UnityEngine.Component { public UnityEngine.GameObject m_destroyedLootPrefab; }
public class OfferingBowl : UnityEngine.Component { public UnityEngine.GameObject m_bossPrefab; public System.Collections.Generic.List<UnityEngine.GameObject> m_spawnPoints; }
public class EffectList
{
    public EffectData[] m_effectPrefabs;
    public class EffectData { public UnityEngine.GameObject m_prefab; }
}
public interface IProjectile { }
public class Projectile : UnityEngine.Component, IProjectile { }
public class Character : UnityEngine.Component { }
public class RandomFlyingBird : UnityEngine.Component { }
public class Fish : UnityEngine.Component { }
public class Ragdoll : UnityEngine.Component { }
public class Pickable : UnityEngine.Component { public UnityEngine.GameObject m_itemPrefab; }
public class PickableItem : UnityEngine.Component { }
public class Ship : UnityEngine.Component { }
public class Vagon : UnityEngine.Component { }
public class Catapult : UnityEngine.Component { }
public class Turret : UnityEngine.Component { }
public class TombStone : UnityEngine.Component { }
public class LootSpawner : UnityEngine.Component { }
public class SpawnArea : UnityEngine.Component { }
public class CinderSpawner : UnityEngine.Component { }
public class WispSpawner : UnityEngine.Component { }
public class SoftReferencePrefabSpawner : UnityEngine.Component { }
public class GrapplingPoint : UnityEngine.Component { }
public class Valkyrie : UnityEngine.Component { }
public class Odin : UnityEngine.Component { }
public class CinematicsManager : UnityEngine.Component { }
public class CinematicsHider : UnityEngine.Component { }
public class EventZone : UnityEngine.Component { }
public class Trader : UnityEngine.Component { }
public class TeleportWorld : UnityEngine.Component { }
public class Teleport : UnityEngine.Component { }
public class RuneStone : UnityEngine.Component { }
public class Vegvisir : UnityEngine.Component { }
public class BossStone : UnityEngine.Component { }
public class Door : UnityEngine.Component { }
public class Smelter : UnityEngine.Component { public ItemConversion[] m_conversion = new ItemConversion[0]; }
public class Fermenter : UnityEngine.Component { public ItemConversion[] m_conversion = new ItemConversion[0]; }
public class CookingStation : UnityEngine.Component { public ItemConversion[] m_conversion = new ItemConversion[0]; }
public class Incinerator : UnityEngine.Component { public IncineratorConversion[] m_conversions = new IncineratorConversion[0]; }
public class Bed : UnityEngine.Component { }
public class Fireplace : UnityEngine.Component { }
public class MapTable : UnityEngine.Component { }
public class Beehive : UnityEngine.Component { }
public class SapCollector : UnityEngine.Component { }
public class LocationProxy : UnityEngine.Component { }
public class TerrainComp : UnityEngine.Component { }
public class TerrainLod : UnityEngine.Component { }
public class EnvZone : UnityEngine.Component { }
public class LiquidVolume : UnityEngine.Component { }
public class MistEmitter : UnityEngine.Component { }
public class ParticleMist : UnityEngine.Component { }
public class Mister : UnityEngine.Component { }
public class TerrainOp : UnityEngine.Component { }
public class TerrainModifier : UnityEngine.Component { public bool m_playerModifiction; }
public class ItemStand : UnityEngine.Component { }
public class Feast : UnityEngine.Component
{
    public System.Collections.Generic.List<FeastLevel> m_feastParts;
    public class FeastLevel { public UnityEngine.GameObject m_onAboveEquals, m_onBelow; }
}
public class SpawnAbility : UnityEngine.Component, IProjectile { public UnityEngine.GameObject[] m_spawnPrefab; }
public class TriggerSpawnAbility : UnityEngine.Component, IProjectile { }
public class CamShaker : UnityEngine.Component { }
public class AnimationEffect : UnityEngine.Component { }
public class ParticleDecal : UnityEngine.Component { }
public class CraftingStation : UnityEngine.Component { }
public class StationExtension : UnityEngine.Component { }
public class LodFadeInOut : UnityEngine.Component { }
public class LightLod : UnityEngine.Component { }
public class SmokeSpawner : UnityEngine.Component { }
public class EffectFade : UnityEngine.Component { }
public class LightFlicker : UnityEngine.Component { }
public class TimedDestruction : UnityEngine.Component { }
public class ZSFX : UnityEngine.Component { }
public class MusicLocation : UnityEngine.Component { }
public class MusicVolume : UnityEngine.Component { }
namespace PingMarkFeatureValidation.Domain
{
    public static class PingTargetResolver
    {
        public static UnityEngine.GameObject GetPrefabRoot(UnityEngine.Collider collider) => collider.Target ?? collider.gameObject;
        public static string GetObjectId(UnityEngine.GameObject target) => System.Text.RegularExpressions.Regex.Replace(
            Utils.GetPrefabName(target), @"\s+\(\d+\)$", "");
    }
    public class ModTree : TreeBase { }
    public class ModPortal : TeleportWorld { }
    public class ModTeleport : Teleport { }
    public class ModEffects : UnityEngine.Component
    {
        private EffectList unrelatedField;
        public ModEffects(UnityEngine.GameObject prefab) { unrelatedField = new EffectList { m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = prefab } } }; }
    }
    public static class Checks
    {
        private static int count;
        private static void Assert(bool condition, string message) { if (!condition) throw new System.Exception(message); count++; }
        public static int Run()
        {
            count = 0;
            UnityEngine.Object.SceneObjects.Clear();
            ZNetScene.instance = new(); ObjectDB.instance = new(); ZoneSystem.instance = new();
            UnityEngine.GameObject Add(string id, params UnityEngine.Component[] components)
            {
                var prefab = new UnityEngine.GameObject(id, components); ZNetScene.instance.m_prefabs.Add(prefab); return prefab;
            }
            var tree = Add("sfx_misleading", new ModTree(), new UnityEngine.LODGroup());
            var rock = Add("alpha", new MineRock5());
            var foliage = Add("beta", new UnityEngine.Renderer());
            var cliff = Add("gamma", new UnityEngine.MeshCollider());
            ZoneSystem.instance.m_vegetation.Add(new() { m_prefab = foliage });
            ZoneSystem.instance.m_vegetation.Add(new() { m_prefab = cliff });
            var material = Add("delta", new ItemDrop());
            var stone = Add("Stone", new ItemDrop());
            var grausten = Add("Grausten", new ItemDrop());
            var ice = Add("Ice", new ItemDrop());
            var crystal = Add("Crystal", new ItemDrop());
            var recipeOutput = Add("recipe_output", new ItemDrop());
            ObjectDB.instance.m_recipes.Add(new()
            {
                m_item = recipeOutput.GetComponent<ItemDrop>(),
                m_resources = new[]
                {
                    new Piece.Requirement { m_resItem = material.GetComponent<ItemDrop>(), m_amount = 1 },
                    new Piece.Requirement { m_resItem = stone.GetComponent<ItemDrop>(), m_amount = 1 },
                    new Piece.Requirement { m_resItem = grausten.GetComponent<ItemDrop>(), m_amount = 1 },
                    new Piece.Requirement { m_resItem = ice.GetComponent<ItemDrop>(), m_amount = 1 },
                    new Piece.Requirement { m_resItem = crystal.GetComponent<ItemDrop>(), m_amount = 1 }
                }
            });
            var unusedRockDrop = Add("unused_rock_drop", new ItemDrop());
            unusedRockDrop.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Equipment;
            var crateMaterial = Add("crate_material", new ItemDrop());
            var ashlandsCrate = Add("dvergrprops_crate_ashlands", new WearNTear { m_supports = true }, new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = crateMaterial } } } });
            var gucksack = Add("Gucksack", new Piece(), new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = material } } } });
            var smallGucksack = Add("Gucksack_small", new Piece(), new WearNTear { m_supports = true }, new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = material } } } });
            var guckDrop = new UnityEngine.GameObject("guck_drop", new Pickable { m_itemPrefab = material });
            var scrapIron = Add("ScrapIron", new ItemDrop());
            var mudpileFragment = new UnityEngine.GameObject("mudpile2_frac", new MineRock5
            {
                m_dropItems = new DropTable { m_drops = new() { new() { m_item = scrapIron } } }
            });
            var mudpile = Add("mudpile", new Destructible { m_spawnWhenDestroyed = mudpileFragment });
            var mudpile2 = Add("mudpile2", new Destructible { m_spawnWhenDestroyed = mudpileFragment });
            var spawnedGuckPile = Add("spawned_guck_pile", new Destructible { m_spawnWhenDestroyed = guckDrop });
            Add("test_smelter", new Smelter
            {
                m_conversion = new[] { new ItemConversion { m_from = scrapIron.GetComponent<ItemDrop>() } }
            });
            var mudScrapPile = Add("muddscrappile", new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = scrapIron } } } });
            var rawFood = Add("raw_food", new ItemDrop());
            rawFood.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
            Add("test_cooking_station", new CookingStation
            {
                m_conversion = new[] { new ItemConversion { m_from = rawFood.GetComponent<ItemDrop>() } }
            });
            var foodPile = Add("food_pile", new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = rawFood } } } });
            var fermentable = Add("fermentable", new ItemDrop());
            Add("test_fermenter", new Fermenter
            {
                m_conversion = new[] { new ItemConversion { m_from = fermentable.GetComponent<ItemDrop>() } }
            });
            var fermentablePile = Add("fermentable_pile", new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = fermentable } } } });
            var incineratorInput = Add("incinerator_input", new ItemDrop());
            Add("test_incinerator", new Incinerator
            {
                m_conversions = new[]
                {
                    new IncineratorConversion
                    {
                        m_requirements = new[]
                        {
                            new Piece.Requirement { m_resItem = incineratorInput.GetComponent<ItemDrop>(), m_amount = 1 }
                        }
                    }
                }
            });
            var incineratorPile = Add("incinerator_pile", new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = incineratorInput } } } });
            var stoneRubble = Add("stone_rubble", new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = stone } } } });
            var graustenDebris = Add("grausten_debris", new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = grausten } } } });
            var iceDebris = Add("ice_debris", new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = ice } } } });
            var crystalDebris = Add("crystal_debris", new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = crystal } } } });
            var deposit = Add("epsilon", new Destructible { m_minToolTier = 1 }, new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = material } } } });
            var rockProp = Add("Rock_4", new UnityEngine.MeshCollider(), new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = unusedRockDrop } } } });
            ZoneSystem.instance.m_vegetation.Add(new() { m_prefab = rockProp });
            var geologicalSource = Add("geological_crafting_source", new UnityEngine.MeshCollider(), new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = material } } } });
            ZoneSystem.instance.m_vegetation.Add(new() { m_prefab = geologicalSource });
            var geologicalDeposit = Add("geological_material_source", new UnityEngine.MeshCollider(),
                new Destructible { m_minToolTier = 1 },
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = material } } } });
            ZoneSystem.instance.m_vegetation.Add(new() { m_prefab = geologicalDeposit });
            var ruin = Add("zeta", new Piece(), new WearNTear { m_supports = true });
            var chest = Add("eta", new Container());
            var seed = Add("weapon_name", new ItemDrop());
            var plant = Add("theta", new Plant(), new Piece { m_resources = new[] { new Piece.Requirement { m_resItem = seed.GetComponent<ItemDrop>(), m_amount = 1 } } });
            var stack = Add("iota", new Piece { m_resources = new[] { new Piece.Requirement { m_resItem = material.GetComponent<ItemDrop>(), m_amount = 50 } } }, new WearNTear());
            var hammer = Add("kappa", new ItemDrop());
            hammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces = new PieceTable { m_pieces = new() { plant, stack } };
            ObjectDB.instance.m_items.Add(hammer);
            var gear = Add("lambda", new ItemDrop());
            ObjectDB.instance.m_recipes.Add(new() { m_item = gear.GetComponent<ItemDrop>(), m_enabled = false });
            var effect = new UnityEngine.GameObject("plain_shape", new UnityEngine.Renderer());
            Add("mu", new ModEffects(effect));
            var replacement = new UnityEngine.GameObject("nu", new UnityEngine.Renderer());
            Add("xi", new Destructible { m_spawnWhenDestroyed = replacement });
            var fragments = new UnityEngine.GameObject("omicron", new UnityEngine.Renderer());
            Add("pi", new WearNTear { m_fragmentRoots = new[] { fragments } });
            var stub = new UnityEngine.GameObject("rho", new TreeLog());
            tree.GetComponent<TreeBase>().m_stubPrefab = stub;
            var generator = new DungeonGenerator { m_algorithm = DungeonGenerator.Algorithm.Dungeon, m_themes = Room.Theme.Modded };
            Add("sigma", generator);
            var room = new UnityEngine.GameObject("tau", new Room { m_theme = Room.Theme.Modded });
            var campRoom = new UnityEngine.GameObject("upsilon", new Room { m_theme = Room.Theme.Camp });
            DungeonDB.instance = new(new DungeonDB.RoomData { prefab = new(room.GetComponent<Room>(), true) },
                new DungeonDB.RoomData { prefab = new(campRoom.GetComponent<Room>(), true) },
                new DungeonDB.RoomData { prefab = new(null, false) }, null);
            var camp = Add("phi", new DungeonGenerator { m_algorithm = DungeonGenerator.Algorithm.CampGrid, m_themes = Room.Theme.Camp });
            var location = Add("chi", new Location { m_hasInterior = true });
            var child = new UnityEngine.GameObject("child", new ZNetView(), new ItemDrop(), new ParticleSystem()); location.Attach(child);
            ZoneSystem.instance.m_locations.Add(new() { m_prefabName = "area_alias", m_prefab = new(location, true) });
            ZoneSystem.instance.m_locations.Add(new() { m_prefabName = "unloaded_alias", m_prefab = new(null, false) });
            var unknown = Add("TrollCave02");
            var audio = Add("psi", new UnityEngine.AudioSource());
            var particles = Add("omega", new ParticleSystem());
            var persistent = Add("rendered_pickup", new Pickable()); persistent.Attach(new("decor", new ParticleSystem()));
            ZoneSystem.instance.m_vegetation.Add(new() { m_prefab = particles });
            var ordinaryLoot = Add("ordinary_loot", new Destructible(), new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = material } } } });
            var portal = Add("opaque_gateway", new ModPortal(), new Piece(), new WearNTear());
            hammer.GetComponent<ItemDrop>().m_itemData.m_shared.m_buildPieces.m_pieces.Add(portal);
            var playerTurret = Add("piece_turret", new Piece(), new Turret());
            var charredBallista = Add("piece_charred_balista", new Piece(), new Turret());
            var lootProp = Add("loot_prop", new Piece(), new Destructible(), new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = material } } } });
            var lootCreature = Add("loot_creature", new Character(), new Destructible(), new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = material } } } });
            var lootContainer = Add("loot_container", new Container(), new Destructible(), new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = material } } } });
            var specific = new System.Collections.Generic.List<(UnityEngine.GameObject Prefab, PrefabBrowserKind Kind)>();
            void Case(UnityEngine.Component component, PrefabBrowserKind kind)
            {
                specific.Add((Add("opaque_" + specific.Count, component), kind));
            }
            Case(new Ship(), PrefabBrowserKind.Vehicle);
            Case(new Vagon(), PrefabBrowserKind.Vehicle);
            Case(new Catapult(), PrefabBrowserKind.Vehicle);
            Case(new Character(), PrefabBrowserKind.Creature);
            Case(new RandomFlyingBird(), PrefabBrowserKind.AmbientCreature);
            Case(new Fish(), PrefabBrowserKind.AmbientCreature);
            Case(new Ragdoll(), PrefabBrowserKind.Ragdoll);
            Case(new CreatureSpawner(), PrefabBrowserKind.Spawner);
            Case(new SpawnArea(), PrefabBrowserKind.Spawner);
            Case(new TriggerSpawner(), PrefabBrowserKind.Spawner);
            Case(new TriggerSpawner(), PrefabBrowserKind.TriggerPoint);
            Case(new EggGrow(), PrefabBrowserKind.Spawner);
            Case(new GrapplingPoint(), PrefabBrowserKind.TriggerPoint);
            Case(new Projectile(), PrefabBrowserKind.Projectile);
            Case(new TerrainOp(), PrefabBrowserKind.TerrainTool);
            Case(new TerrainModifier { m_playerModifiction = true }, PrefabBrowserKind.TerrainTool);
            Case(new LocationProxy(), PrefabBrowserKind.SystemController);
            Case(new TerrainComp(), PrefabBrowserKind.SystemController);
            Case(new LiquidVolume(), PrefabBrowserKind.SystemController);
            Case(new TombStone(), PrefabBrowserKind.PlayerDeath);
            Case(new OfferingBowl { m_bossPrefab = specific[3].Prefab }, PrefabBrowserKind.BossTrigger);
            Case(new EventZone(), PrefabBrowserKind.SystemController);
            Case(new Valkyrie(), PrefabBrowserKind.Cinematic);
            Case(new Odin(), PrefabBrowserKind.Cinematic);
            Case(new CinematicsManager(), PrefabBrowserKind.Cinematic);
            Case(new ItemStand(), PrefabBrowserKind.Prop);
            Case(new Feast(), PrefabBrowserKind.Prop);
            Case(new PickableItem(), PrefabBrowserKind.Pickable);
            Case(new LightLod(), PrefabBrowserKind.Lod);
            Case(new EffectFade(), PrefabBrowserKind.VisualFx);
            Case(new MusicLocation(), PrefabBrowserKind.Audio);
            Case(new CamShaker(), PrefabBrowserKind.VisualFx);
            Case(new AnimationEffect(), PrefabBrowserKind.VisualFx);
            Case(new ParticleDecal(), PrefabBrowserKind.Particle);
            Case(new SpawnAbility(), PrefabBrowserKind.Spawner);
            Case(new TriggerSpawnAbility(), PrefabBrowserKind.TriggerPoint);
            Case(new Piece(), PrefabBrowserKind.BuildingPiece);
            Case(new TreeLog(), PrefabBrowserKind.Tree);
            Case(new Trader(), PrefabBrowserKind.Useful);
            Case(new TeleportWorld(), PrefabBrowserKind.Portal);
            Case(new Teleport(), PrefabBrowserKind.Portal);
            Case(new ModPortal(), PrefabBrowserKind.Portal);
            Case(new ModTeleport(), PrefabBrowserKind.Portal);
            var embeddedAmmo = Add("siege_with_embedded_ammo", new Catapult());
            embeddedAmmo.Attach(new("ammo_visual", new ItemDrop()));
            var worldRoof = Add("world_roof_without_piece", new WearNTear { m_supports = true });
            var registeredVillage = Add("registered_village", new WearNTear { m_supports = true });
            ZoneSystem.instance.m_locations.Add(new() { m_prefabName = "village_alias", m_prefab = new(registeredVillage, true) });
            var feastVisual = new UnityEngine.GameObject("unrelated_visual", new UnityEngine.Renderer());
            Add("serving_stage", new Feast { m_feastParts = new() { new() { m_onAboveEquals = feastVisual } } });
            var intactCamp = Add("intact_camp", new Location(), new DungeonGenerator { m_algorithm = DungeonGenerator.Algorithm.CampGrid });
            var crystals = new UnityEngine.GameObject("crystals"); room.Attach(crystals);
            var embeddedRock = new UnityEngine.GameObject("caverock_rockbun (1)"); crystals.Attach(embeddedRock);
            embeddedRock.Attach(new("default", new UnityEngine.MeshCollider { Target = embeddedRock }));
            var dungeonMudpile = new UnityEngine.GameObject("mudpile2(Clone)", new Destructible(),
                new DropOnDestroyed { m_dropWhenDestroyed = new DropTable { m_drops = new() { new() { m_item = scrapIron } } } });
            room.Attach(dungeonMudpile);
            dungeonMudpile.Attach(new("base_model", new UnityEngine.MeshCollider { Target = dungeonMudpile }));
            var duplicateRock = new UnityEngine.GameObject("caverock_rockbun (2)"); crystals.Attach(duplicateRock);
            duplicateRock.Attach(new("default", new UnityEngine.MeshCollider { Target = duplicateRock }));
            var roomPickup = new UnityEngine.GameObject("rendered_pickup", new ZNetView(), new Pickable()); room.Attach(roomPickup);
            roomPickup.Attach(new("default", new UnityEngine.Collider { Target = roomPickup }));
            var roomCollider = new UnityEngine.Collider { Target = room }; room.Add(roomCollider);
            var liveDungeon = new UnityEngine.GameObject("live_dungeon", new DungeonGenerator
                { m_algorithm = DungeonGenerator.Algorithm.Dungeon });
            var liveRoom = new UnityEngine.GameObject("unloaded_room_instance", new Room { m_theme = Room.Theme.Camp });
            liveDungeon.Attach(liveRoom);
            var liveRock = new UnityEngine.GameObject("live_room_rock (1)"); liveRoom.Attach(liveRock);
            liveRock.Attach(new("default", new UnityEngine.Collider { Target = liveRock }));
            var liveCrystal = new UnityEngine.GameObject("live_crystal(Clone)", new Pickable()); liveRoom.Attach(liveCrystal);
            liveCrystal.Attach(new("interaction", new UnityEngine.Collider { Target = liveCrystal }));
            var liveItem = new UnityEngine.GameObject("live_pickable_item(Clone)", new PickableItem());
            UnityEngine.Object.SceneObjects.AddRange(new UnityEngine.Object[]
                { liveRoom.GetComponent<Room>(), liveCrystal.GetComponent<Pickable>(), liveItem.GetComponent<PickableItem>() });
            var filter = new PrefabBrowserFilter();
            string[] names = new PrefabFeatureClassifier(filter).Build();
            var steppedFilter = new PrefabBrowserFilter();
            var steppedClassifier = new PrefabFeatureClassifier(steppedFilter);
            var originalScene = ZNetScene.instance;
            ZNetScene.instance = null;
            var loading = steppedClassifier.BuildSteps().GetEnumerator();
            Assert(steppedClassifier.Names().Length == 0, "Creating loading steps does not scan Unity objects");
            ZNetScene.instance = originalScene;
            Assert(loading.MoveNext() && loading.Current == 1 && steppedClassifier.Names().Length == 1,
                "First loading step yields after one prefab instead of classifying the entire registry");
            loading.Dispose();
            Assert(!loading.MoveNext() && steppedClassifier.Names().Length == 1, "Disposed loading cannot continue scanning");
            var comparisonFilter = new PrefabBrowserFilter();
            var comparisonNames = new PrefabFeatureClassifier(comparisonFilter).Build();
            steppedFilter = new PrefabBrowserFilter();
            steppedClassifier = new PrefabFeatureClassifier(steppedFilter);
            int steps = 0;
            foreach (int progress in steppedClassifier.BuildSteps()) steps++;
            Assert(steps > comparisonNames.Length, "Classification yields during reference and hierarchy scans too");
            Assert(System.Linq.Enumerable.SequenceEqual(comparisonNames, steppedClassifier.Names()),
                "Incremental and synchronous discovery produce identical names");
            foreach (string id in comparisonNames)
                foreach (PrefabBrowserKind kind in System.Enum.GetValues(typeof(PrefabBrowserKind)))
                    Assert(comparisonFilter.Includes(id, PrefabBrowserKind.None, kind)
                        == steppedFilter.Includes(id, PrefabBrowserKind.None, kind), "Incremental classification retains category evidence");
            var collections = new PrefabCollections(names, tree.name + ", " + rock.name, "", "");
            var visibleTrees = System.Linq.Enumerable.Where(collections.Names(1, ""),
                id => filter.Includes(id, PrefabBrowserKind.None, PrefabBrowserKind.Vegetation));
            collections.Move(visibleTrees, 3);
            Assert(collections.Count(3) == 1 && collections.Names(3, "")[0] == tree.name,
                "Moving a section's visible selection respects feature filters");
            Assert(collections.Names(1, "")[0] == rock.name, "Hidden section items remain in their original collection");
            Assert(collections.Undo() && collections.Count(1) == 2, "Section selection moves remain undoable");
            bool Tagged(UnityEngine.GameObject prefab, PrefabBrowserKind kind) => filter.Includes(prefab.name, PrefabBrowserKind.None, kind);
            Assert(Tagged(embeddedAmmo, PrefabBrowserKind.Vehicle) && !Tagged(embeddedAmmo, PrefabBrowserKind.DroppedItem),
                "Embedded ItemDrop does not turn a vehicle into a dropped-item prefab");
            Assert(Tagged(tree, PrefabBrowserKind.Tree), "Derived tree has explicit harvestable-tree evidence");
            Assert(!Tagged(location, PrefabBrowserKind.BuildingPiece), "Whole locations are not building-piece models");
            Assert(Tagged(worldRoof, PrefabBrowserKind.BuildingPiece), "World structural models without Piece are building parts");
            Assert(Tagged(registeredVillage, PrefabBrowserKind.Location) && !Tagged(registeredVillage, PrefabBrowserKind.BuildingPiece),
                "Registry-marked whole village is not excluded as a structural part");
            Assert(System.Linq.Enumerable.Count(names, name => name == "caverock_rockbun") == 1,
                "Embedded dungeon object is listed once with resolver's stable ID");
            Assert(System.Array.IndexOf(names, "caverock_rockbun (1)") < 0 && System.Array.IndexOf(names, "default") < 0,
                "Embedded collider and instance suffix are not listed as separate targets");
            Assert(filter.Includes("caverock_rockbun", PrefabBrowserKind.None, PrefabBrowserKind.Dungeon), "Embedded object is a dungeon asset");
            Assert(filter.Includes("caverock_rockbun", PrefabBrowserKind.None, PrefabBrowserKind.Prop), "Embedded geometry is a prop");
            Assert(!filter.Includes("caverock_rockbun", PrefabBrowserKind.Dungeon), "Dungeon Hide applies to embedded objects");
            Assert(Tagged(persistent, PrefabBrowserKind.Dungeon) && !Tagged(persistent, PrefabBrowserKind.Prop),
                "Registered dungeon pickup retains existing role instead of becoming an embedded prop");
            Assert(System.Array.IndexOf(names, "live_room_rock") >= 0,
                "Active dungeon room geometry included without original room asset or matching registered theme");
            Assert(System.Array.IndexOf(names, "mudpile2") >= 0
                && filter.Includes("mudpile2", PrefabBrowserKind.None, PrefabBrowserKind.Resource)
                && PrefabAutoSort.Category("mudpile2", filter.Kind("mudpile2")) == 0,
                "Unregistered dungeon scrap piles use their live crafting-resource drop evidence");
            Assert(System.Array.IndexOf(names, "live_crystal") >= 0 && System.Array.IndexOf(names, "live_crystal(Clone)") < 0,
                "Live crystal discovered with canonical ID, not clone name");
            Assert(filter.Includes("live_crystal", PrefabBrowserKind.None, PrefabBrowserKind.Pickable)
                && filter.Includes("live_crystal", PrefabBrowserKind.None, PrefabBrowserKind.Dungeon),
                "Live dungeon crystal is classified as pickable and dungeon object");
            Assert(!filter.Includes("live_crystal", PrefabBrowserKind.None, PrefabBrowserKind.Prop),
                "Embedded pickable is not mislabeled as decorative prop");
            Assert(System.Array.IndexOf(names, "live_pickable_item") >= 0
                && filter.Includes("live_pickable_item", PrefabBrowserKind.None, PrefabBrowserKind.Pickable),
                "Live PickableItem discovered independently of dungeon database");
            Assert(Tagged(tree, PrefabBrowserKind.Vegetation), "Derived mod tree");
            Assert(!Tagged(tree, PrefabBrowserKind.Audio), "Name must not imply audio");
            Assert(!Tagged(tree, PrefabBrowserKind.VisualFx), "Name must not imply FX");
            Assert(Tagged(rock, PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable), "MineRock5 roles");
            Assert(Tagged(rock, PrefabBrowserKind.Fracture), "Multipart fracture feature");
            Assert(Tagged(rockProp, PrefabBrowserKind.Geology), "Natural rock geometry");
            Assert(!Tagged(rockProp, PrefabBrowserKind.Mineable) && !Tagged(rockProp, PrefabBrowserKind.Resource)
                && PrefabAutoSort.Category(rockProp.name, filter.Kind(rockProp.name)) == -1,
                "Generic geology is excluded from resource auto-sort");
            Assert(Tagged(geologicalDeposit, PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable | PrefabBrowserKind.Resource)
                && PrefabAutoSort.Category(geologicalDeposit.name, filter.Kind(geologicalDeposit.name)) == 0,
                "Tool-required geological material sources auto-sort to Valuables");
            Assert(Tagged(geologicalSource, PrefabBrowserKind.Geology | PrefabBrowserKind.Resource)
                && !Tagged(geologicalSource, PrefabBrowserKind.Mineable)
                && PrefabAutoSort.Category(geologicalSource.name, filter.Kind(geologicalSource.name)) == 0,
                "Crafting-resource geology auto-sorts without a tool-tier requirement");
            Assert(Tagged(foliage, PrefabBrowserKind.Vegetation), "Natural registry foliage");
            Assert(Tagged(cliff, PrefabBrowserKind.Geology), "Natural registry solid geology");
            Assert(Tagged(deposit, PrefabBrowserKind.Mineable), "Material-drop deposit");
            Assert(PrefabAutoSort.Category(deposit.name, filter.Kind(deposit.name)) == 0,
                "Non-geology material deposit still auto-sorts to Valuables");
            Assert(PrefabAutoSort.Category("Spawner_CharredStone",
                PrefabBrowserKind.Spawner | PrefabBrowserKind.Resource) == 2,
                "Spawner stays in Useful when it also has resource evidence");
            Assert(PrefabAutoSort.Category("RuneStone_Swamps40a1de4c190bedf42acf975f34a18b42",
                PrefabBrowserKind.Useful | PrefabBrowserKind.Location) == 2,
                "RuneStone stays Useful when it is also a Location");
            Assert(PrefabAutoSort.Category("Vegvisir_DNBoss",
                PrefabBrowserKind.Useful | PrefabBrowserKind.Resource) == 2,
                "Vegvisir stays Useful when it also has resource evidence");
            Assert(PrefabAutoSort.Category("offferaltar_memorialsite",
                PrefabBrowserKind.Useful | PrefabBrowserKind.Location) == 2,
                "Offering objects stay Useful when they are also Locations");
            foreach (string usefulPrefab in new[]
                {
                    "RuneStone_Memorial1", "Vegvisir_DNBoss", "offferaltar_memorialsite",
                    "RuneTablet_Fader", "Offeraltar_fader", "fader_bellholder"
                })
                Assert(PrefabAutoSort.Category(usefulPrefab, PrefabBrowserKind.None) == 2,
                    usefulPrefab + " fallback auto-sorts to Useful");
            foreach (string locationPrefab in new[]
                { "CharredFortress", "CharredRuins2", "CharredRuins3", "MorgenHole1", "MorgenHole3" })
                Assert(PrefabAutoSort.Category(locationPrefab, PrefabBrowserKind.Useful | PrefabBrowserKind.Location) == 1,
                    locationPrefab + " remains in Locations despite Useful component evidence");
            string[] locationAutoSortNames =
            ["CharredFortress", "CharredRuins2", "CharredRuins3", "MorgenHole1", "MorgenHole3"];
            var locationAutoSortFilter = new PrefabBrowserFilter();
            foreach (string name in locationAutoSortNames)
                locationAutoSortFilter.Register(name, PrefabBrowserKind.Useful);
            var locationAutoSortSections = PrefabAutoSort.CreateSections(locationAutoSortNames, locationAutoSortFilter);
            var sortedLocations = new System.Collections.Generic.HashSet<string>(
                System.Linq.Enumerable.Select(locationAutoSortSections[1].Prefabs.Split(','), name => name.Trim()),
                System.StringComparer.OrdinalIgnoreCase);
            foreach (string name in locationAutoSortNames)
                Assert(sortedLocations.Contains(name), name + " is placed in Locations by CreateSections");
            string[] usefulAutoSortNames =
            ["RuneStone_Memorial1", "Vegvisir_DNBoss", "offferaltar_memorialsite", "RuneTablet_Fader", "Offeraltar_fader", "fader_bellholder"];
            var usefulAutoSortSections = PrefabAutoSort.CreateSections(usefulAutoSortNames, new PrefabBrowserFilter());
            var sortedUseful = new System.Collections.Generic.HashSet<string>(
                System.Linq.Enumerable.Select(usefulAutoSortSections[2].Prefabs.Split(','), name => name.Trim()),
                System.StringComparer.OrdinalIgnoreCase);
            foreach (string name in usefulAutoSortNames)
                Assert(sortedUseful.Contains(name), name + " is placed in Useful by CreateSections");
            var resourceNames = new[]
            {
                ashlandsCrate.name, gucksack.name, smallGucksack.name, mudScrapPile.name, foodPile.name,
                fermentablePile.name, incineratorPile.name, mudpile.name, mudpile2.name,
                spawnedGuckPile.name, crystalDebris.name, stoneRubble.name, graustenDebris.name, iceDebris.name
            };
            var resourceSections = PrefabAutoSort.CreateSections(resourceNames, filter);
            var valuables = new System.Collections.Generic.HashSet<string>(
                System.Linq.Enumerable.Select(resourceSections[0].Prefabs.Split(','), name => name.Trim()),
                System.StringComparer.OrdinalIgnoreCase);
            int expectedResourceCount = resourceNames.Length - 3;
            foreach (string resourceName in System.Linq.Enumerable.Take(resourceNames, expectedResourceCount))
                Assert(valuables.Contains(resourceName), resourceName + " auto-sorts to Valuables from its consumed drop");
            foreach (string excludedName in System.Linq.Enumerable.Skip(resourceNames, expectedResourceCount))
                Assert(!valuables.Contains(excludedName), excludedName + " drops are excluded by prefab ID");
            Assert(Tagged(portal, PrefabBrowserKind.Portal | PrefabBrowserKind.Useful | PrefabBrowserKind.Buildable), "Buildable derived portal retains portal and utility roles");
            Assert(PrefabAutoSort.Category(portal.name, filter.Kind(portal.name)) == 2, "Buildable portal auto-sorts to Useful");
            foreach (UnityEngine.GameObject turret in new[] { playerTurret, charredBallista })
            {
                Assert(Tagged(turret, PrefabBrowserKind.Turret | PrefabBrowserKind.Useful), turret.name + " has the Turrets filter");
                Assert(filter.Includes(turret.name, PrefabBrowserKind.None, PrefabBrowserKind.Turret), turret.name + " appears in Turrets filter");
                Assert(PrefabAutoSort.Category(turret.name, filter.Kind(turret.name)) == 2, turret.name + " auto-sorts to Useful");
            }
            Assert(!filter.Includes(ruin.name, PrefabBrowserKind.None, PrefabBrowserKind.Portal), "Portal-only filter excludes ordinary structures");
            Assert(Tagged(ruin, PrefabBrowserKind.Ruin), "Structural world wear");
            Assert(!Tagged(ruin, PrefabBrowserKind.Prop), "No generic prop on structural ruin");
            Assert(Tagged(chest, PrefabBrowserKind.Container), "Container feature");
            Assert(Tagged(seed, PrefabBrowserKind.Seed), "Planting requirements rather than seed names");
            Assert(Tagged(plant, PrefabBrowserKind.Planting), "Planting component");
            Assert(Tagged(stack, PrefabBrowserKind.Stack), "Bulk material construction feature");
            Assert(Tagged(stack, PrefabBrowserKind.Buildable), "Build registry");
            Assert(Tagged(gear, PrefabBrowserKind.Craftable), "Recipe output");
            Assert(Tagged(gear, PrefabBrowserKind.Development), "Disabled recipe metadata");
            Assert(Tagged(effect, PrefabBrowserKind.VisualFx), "Private mod EffectList reference");
            Assert(Tagged(replacement, PrefabBrowserKind.Destruction), "Destroyed replacement relation");
            Assert(Tagged(fragments, PrefabBrowserKind.Fracture), "Fragment relation");
            Assert(Tagged(fragments, PrefabBrowserKind.Destruction), "Fragment destruction context");
            Assert(Tagged(stub, PrefabBrowserKind.Vegetation), "Tree stump relation");
            Assert(Tagged(room, PrefabBrowserKind.Dungeon), "Modded room theme");
            Assert(!Tagged(campRoom, PrefabBrowserKind.Dungeon), "Camp room is not a dungeon");
            Assert(!Tagged(camp, PrefabBrowserKind.Dungeon), "Camp generator is not a dungeon");
            Assert(Tagged(camp, PrefabBrowserKind.SystemController), "Standalone outdoor generator is a system helper");
            Assert(Tagged(intactCamp, PrefabBrowserKind.Location), "Whole camp keeps its location role");
            Assert(!Tagged(intactCamp, PrefabBrowserKind.SystemController), "Whole camp is not a standalone generator helper");
            Assert(Tagged(feastVisual, PrefabBrowserKind.Prop), "Feast visual references are props");
            Assert(filter.Includes("area_alias", PrefabBrowserKind.None, PrefabBrowserKind.Dungeon), "Loaded location alias");
            Assert(filter.Includes("unloaded_alias", PrefabBrowserKind.None, PrefabBrowserKind.Location), "Unloaded location preserved");
            Assert(!Tagged(location, PrefabBrowserKind.DroppedItem), "Nested network entity boundary");
            Assert(!Tagged(unknown, PrefabBrowserKind.Dungeon), "No dungeon guessing from name");
            Assert(Tagged(audio, PrefabBrowserKind.Audio), "Standard Unity audio feature");
            Assert(Tagged(particles, PrefabBrowserKind.Particle), "Standard Unity particles");
            Assert(!Tagged(persistent, PrefabBrowserKind.Particle), "Embedded particles not standalone FX");
            Assert(!Tagged(particles, PrefabBrowserKind.Vegetation), "Natural effects are not plants");
            Assert(!Tagged(ordinaryLoot, PrefabBrowserKind.Mineable), "Ordinary loot props are not mineral deposits");
            Assert(Tagged(ordinaryLoot, PrefabBrowserKind.Resource), "Gucksack-like material drops are resources without a tool tier");
            Assert(!Tagged(lootProp, PrefabBrowserKind.Resource), "Destructible building props are not resources");
            Assert(!Tagged(lootCreature, PrefabBrowserKind.Resource), "Material-dropping creatures are not resources");
            Assert(!Tagged(lootContainer, PrefabBrowserKind.Resource), "Material-dropping containers are not resources");
            foreach (var entry in specific)
            {
                Assert(Tagged(entry.Prefab, entry.Kind), "Feature classification of " + entry.Prefab.name);
                Assert(!filter.Includes(entry.Prefab.name, entry.Kind), "Feature Hide of " + entry.Prefab.name);
                if (entry.Kind == PrefabBrowserKind.Portal)
                    Assert(PrefabAutoSort.Category(entry.Prefab.name, filter.Kind(entry.Prefab.name)) == 2, "Teleport component auto-sorts to Useful");
            }
            foreach (string id in names)
                Assert(!filter.Includes(id, PrefabBrowserKind.All, PrefabBrowserKind.None), "None hides every classified and unknown name");
            Assert(!filter.Includes("not_registered", PrefabBrowserKind.All, PrefabBrowserKind.None), "None hides unknown names too");
            Assert(System.Array.IndexOf(names, effect.name) >= 0, "Referenced assets included");
            Assert(filter.Includes(unknown.name, PrefabBrowserKind.None, PrefabBrowserKind.Uncategorized), "Unknown mod object remains Other");
            foreach (PrefabBrowserKind kind in System.Enum.GetValues(typeof(PrefabBrowserKind)))
            {
                if (kind == PrefabBrowserKind.None || kind == PrefabBrowserKind.All) continue;
                ulong value = (ulong)kind;
                Assert((value & (value - 1)) == 0, "Unique 64-bit category");
                Assert((PrefabBrowserKind.All & kind) == kind, "All includes category");
            }
            int rename = 0;
            foreach (UnityEngine.GameObject prefab in ZNetScene.instance.m_prefabs) prefab.name = "random_mod_id_" + rename++;
            var renamedFilter = new PrefabBrowserFilter();
            new PrefabFeatureClassifier(renamedFilter).Build();
            foreach (var entry in specific)
                Assert(renamedFilter.Includes(entry.Prefab.name, PrefabBrowserKind.None, entry.Kind), "Renaming does not affect category");
            Assert(renamedFilter.Includes(tree.name, PrefabBrowserKind.None, PrefabBrowserKind.Vegetation), "Renamed derived tree");
            Assert(renamedFilter.Includes(seed.name, PrefabBrowserKind.None, PrefabBrowserKind.Seed), "Renamed planting input");
            Assert(renamedFilter.Includes(portal.name, PrefabBrowserKind.None, PrefabBrowserKind.Portal), "Renamed mod portal remains in portal filter");
            Assert(PrefabAutoSort.Category(portal.name, renamedFilter.Kind(portal.name)) == 2, "Renamed mod portal auto-sorts to Useful");
            return count;
        }
    }
}