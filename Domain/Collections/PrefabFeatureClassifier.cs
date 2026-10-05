using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JoksterCube.PingMark.Domain.Enums;
using JoksterCube.PingMark.Domain.PingTargets;
using SoftReferenceableAssets;
using UnityEngine;

namespace JoksterCube.PingMark.Domain.Collections;

internal sealed class PrefabFeatureClassifier(PrefabBrowserFilter filter)
{
    private static readonly Dictionary<Type, FieldInfo[]> Fields = [];
    private static readonly HashSet<string> EnvironmentalResourceExclusions = new(StringComparer.OrdinalIgnoreCase)
        { "Stone", "Grausten", "Ice" };
    private readonly List<GameObject> _prefabs = [];
    private readonly HashSet<GameObject> _known = [];
    private readonly Dictionary<string, GameObject> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<GameObject, Component[]> _components = [];
    private readonly Dictionary<GameObject, PrefabBrowserKind> _evidence = [];
    private readonly HashSet<GameObject> _nature = [];
    private readonly HashSet<string> _resourceInputs = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Name, GameObject Prefab)> _locationAliases = [];
    private Room.Theme _dungeonThemes;

    internal string[] Build()
    {
        foreach (int progress in BuildSteps()) { }
        return Names();
    }

    internal string[] Names() => _names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();

    internal IEnumerable<int> BuildSteps()
    {
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs.Concat(ZNetScene.instance.m_nonNetViewPrefabs))
        {
            Add(prefab);
            yield return _names.Count;
        }
        if (ObjectDB.instance)
        {
            HashSet<PieceTable> tables = [];
            foreach (GameObject item in ObjectDB.instance.m_items)
            {
                Add(item);
                yield return _names.Count;
                PieceTable? table = item ? item.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces : null;
                if (!table || !tables.Add(table!)) continue;
                foreach (GameObject prefab in table!.m_pieces)
                {
                    yield return _names.Count;
                    if (!prefab) continue;
                    Mark(prefab, PrefabBrowserKind.Buildable);
                    Piece? piece = prefab.GetComponent<Piece>();
                    if (piece && !piece!.m_enabled) Mark(prefab, PrefabBrowserKind.Development);
                    if (!piece || piece!.m_resources == null) continue;
                    bool planting = prefab.GetComponentInChildren<Plant>(true);
                    foreach (Piece.Requirement requirement in piece.m_resources)
                        if (requirement is { } && requirement.m_resItem && requirement.m_amount > 0)
                        {
                            RegisterResourceInput(requirement.m_resItem.gameObject);
                            if (planting) Mark(requirement.m_resItem.gameObject, PrefabBrowserKind.Seed);
                        }
                }
            }
            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe && recipe.m_item)
                    Mark(recipe.m_item.gameObject, PrefabBrowserKind.Craftable
                        | (!recipe.m_enabled ? PrefabBrowserKind.Development : PrefabBrowserKind.None));
                if (recipe && recipe.m_enabled) RegisterResourceInputs(recipe, "m_resources", "m_resItem");
                yield return _names.Count;
            }
        }
        if (ZoneSystem.instance)
        {
            foreach (ZoneSystem.ZoneVegetation vegetation in ZoneSystem.instance.m_vegetation)
            {
                if (vegetation.m_prefab) { Add(vegetation.m_prefab); _nature.Add(vegetation.m_prefab); }
                yield return _names.Count;
            }
            foreach (ZoneSystem.ZoneLocation location in ZoneSystem.instance.m_locations)
            {
                yield return _names.Count;
                string name = string.IsNullOrWhiteSpace(location.m_prefabName) ? location.m_name : location.m_prefabName;
                if (string.IsNullOrWhiteSpace(name)) continue;
                _names.Add(name);
                filter.RegisterLocation(name, location.m_interiorRadius > 0);
                if (!location.m_prefab.IsLoaded) continue;
                GameObject prefab = location.m_prefab.Asset;
                if (!prefab) continue;
                Mark(prefab, PrefabBrowserKind.Location);
                _locationAliases.Add((name, prefab));
            }
            Mark(ZoneSystem.instance.m_zonePrefab, PrefabBrowserKind.SystemController);
            Mark(ZoneSystem.instance.m_zoneCtrlPrefab, PrefabBrowserKind.SystemController);
            Mark(ZoneSystem.instance.m_locationProxyPrefab, PrefabBrowserKind.SystemController);
        }
        foreach (int progress in IndexLoadedRooms()) yield return progress;
        foreach (int progress in IndexResourceInputs()) yield return progress;
        for (int index = 0; index < _prefabs.Count; index++)
        {
            GameObject prefab = _prefabs[index];
            Component[] components = Components(prefab);
            Mark(prefab, Classify(prefab, components));
            yield return _names.Count;
            foreach (int progress in IndexReferences(prefab, components)) yield return progress;
        }
        foreach (GameObject prefab in _prefabs.ToArray())
        {
            yield return _names.Count;
            foreach (Room room in Components(prefab).OfType<Room>())
                if ((room.m_theme & _dungeonThemes) != 0)
                {
                    Mark(prefab, PrefabBrowserKind.Dungeon);
                    foreach (int progress in IndexHierarchy(prefab, true)) yield return progress;
                }
        }
        yield return _names.Count;
        foreach (Room room in UnityEngine.Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
        {
            yield return _names.Count;
            DungeonGenerator? generator = room.GetComponentInParent<DungeonGenerator>();
            if ((room.m_theme & _dungeonThemes) == 0
                && (!generator || generator!.m_algorithm != DungeonGenerator.Algorithm.Dungeon)) continue;
            foreach (int progress in IndexHierarchy(room.gameObject, true)) yield return progress;
        }
        yield return _names.Count;
        foreach (Pickable pickable in UnityEngine.Object.FindObjectsByType<Pickable>(FindObjectsSortMode.None))
        {
            if (pickable) RegisterScenePickable(pickable);
            yield return _names.Count;
        }
        yield return _names.Count;
        foreach (PickableItem pickable in UnityEngine.Object.FindObjectsByType<PickableItem>(FindObjectsSortMode.None))
        {
            if (pickable) RegisterScenePickable(pickable);
            yield return _names.Count;
        }
        foreach ((string name, GameObject prefab) in _locationAliases)
        {
            filter.Register(name, _evidence[prefab]);
            yield return _names.Count;
        }
    }

    private PrefabBrowserKind Classify(GameObject prefab, Component[] components)
    {
        _evidence.TryGetValue(prefab, out PrefabBrowserKind kind);
        bool Has<T>() => components.OfType<T>().Any();
        Piece? piece = components.OfType<Piece>().FirstOrDefault();
        WearNTear? wear = components.OfType<WearNTear>().FirstOrDefault();
        Destructible? destructible = components.OfType<Destructible>().FirstOrDefault();
        Location? location = components.OfType<Location>().FirstOrDefault();
        DungeonGenerator? generator = components.OfType<DungeonGenerator>().FirstOrDefault();
        bool buildable = (kind & PrefabBrowserKind.Buildable) != 0;
        bool planting = Has<Plant>();
        bool terrain = Has<TerrainOp>() || components.OfType<TerrainModifier>().Any(modifier => modifier.m_playerModifiction);
        if ((piece || wear) && !location && !generator && !Has<Room>()
            && (kind & PrefabBrowserKind.Location) == 0) kind |= PrefabBrowserKind.BuildingPiece;
        if (planting) kind |= PrefabBrowserKind.Planting | PrefabBrowserKind.Vegetation;
        if (terrain) kind |= PrefabBrowserKind.TerrainTool;
        if (Has<ItemStand>() || Has<Feast>()) kind |= PrefabBrowserKind.Prop;
        if (prefab.GetComponent<ItemDrop>()) kind |= PrefabBrowserKind.DroppedItem;
        if (Has<Pickable>() || Has<PickableItem>()) kind |= PrefabBrowserKind.Pickable;
        if (Has<TeleportWorld>() || Has<Teleport>()) kind |= PrefabBrowserKind.Portal;
        if (Has<Turret>()) kind |= PrefabBrowserKind.Turret | PrefabBrowserKind.Useful;
        if (Has<Trader>() || Has<TeleportWorld>() || Has<Teleport>() || Has<RuneStone>()
            || Has<Vegvisir>() || Has<BossStone>() || Has<Door>() || Has<OfferingBowl>()
            || Has<CraftingStation>() || Has<StationExtension>() || Has<CreatureSpawner>() || Has<SpawnArea>()
            || Has<TriggerSpawner>() || Has<CinderSpawner>() || Has<WispSpawner>() || Has<EggGrow>()
            || Has<Smelter>() || Has<Fermenter>() || Has<CookingStation>() || Has<Incinerator>()
            || Has<Bed>() || Has<Fireplace>() || Has<MapTable>())
            kind |= PrefabBrowserKind.Useful;
        if (Has<TreeBase>() || Has<TreeLog>() || Has<Beehive>() || Has<SapCollector>() || components.OfType<ItemDrop>().Any(item =>
            item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material)) kind |= PrefabBrowserKind.Resource;
        if (Has<Character>()) kind |= PrefabBrowserKind.Creature;
        if (Has<RandomFlyingBird>() || Has<Fish>()) kind |= PrefabBrowserKind.AmbientCreature;
        if (Has<Ragdoll>()) kind |= PrefabBrowserKind.Ragdoll;
        if (Has<Ship>() || Has<Vagon>() || Has<Catapult>()) kind |= PrefabBrowserKind.Vehicle;
        if (Has<TombStone>()) kind |= PrefabBrowserKind.PlayerDeath;
        if (Has<Container>() || Has<LootSpawner>()) kind |= PrefabBrowserKind.Container;
        if (Has<IProjectile>()) kind |= PrefabBrowserKind.Projectile;
        if (Has<CreatureSpawner>() || Has<SpawnArea>() || Has<TriggerSpawner>() || Has<LootSpawner>()
            || Has<CinderSpawner>() || Has<WispSpawner>() || Has<SoftReferencePrefabSpawner>() || Has<EggGrow>() || Has<SpawnAbility>())
            kind |= PrefabBrowserKind.Spawner;
        if (Has<TriggerSpawner>() || Has<GrapplingPoint>() || Has<TriggerSpawnAbility>()) kind |= PrefabBrowserKind.TriggerPoint;
        if (location) kind |= PrefabBrowserKind.Location;
        if (location && location!.m_hasInterior || generator && generator!.m_algorithm == DungeonGenerator.Algorithm.Dungeon)
            kind |= PrefabBrowserKind.Dungeon;
        if (generator) IndexDungeonTheme(generator!);
        if (Has<Valkyrie>() || Has<Odin>() || Has<CinematicsManager>() || Has<CinematicsHider>()) kind |= PrefabBrowserKind.Cinematic;
        if (components.OfType<OfferingBowl>().Any(bowl => bowl.m_bossPrefab)) kind |= PrefabBrowserKind.BossTrigger;
        if (Has<LocationProxy>() || Has<TerrainComp>() || Has<TerrainLod>() || Has<EnvZone>()
            || Has<LiquidVolume>() || Has<MistEmitter>() || Has<ParticleMist>() || Has<Mister>()
            || Has<EventZone>() && !location && !Has<OfferingBowl>()
            || generator && !location && generator!.m_algorithm != DungeonGenerator.Algorithm.Dungeon)
            kind |= PrefabBrowserKind.SystemController;
        if (Has<TreeBase>() || Has<TreeLog>() || destructible && (destructible!.m_destructibleType & DestructibleType.Tree) != 0)
            kind |= PrefabBrowserKind.Vegetation | PrefabBrowserKind.Tree | PrefabBrowserKind.Resource;
        if (Has<MineRock>() || Has<MineRock5>()) kind |= PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable;
        if (Has<MineRock5>()) kind |= PrefabBrowserKind.Fracture;
        if (_nature.Contains(prefab) && kind == PrefabBrowserKind.None && !Has<ParticleSystem>()
            && !Has<AudioSource>() && !Has<ZSFX>() && !Has<TimedDestruction>() && !Has<EffectFade>())
            kind |= Has<MeshCollider>() ? PrefabBrowserKind.Geology : PrefabBrowserKind.Vegetation;
        bool resourceDrops = components.OfType<DropOnDestroyed>().Any(drop => HasResourceDrops(drop.m_dropWhenDestroyed))
            || destructible && HasResourceDropPrefab(destructible!.m_spawnWhenDestroyed);
        const PrefabBrowserKind resourceExclusions = PrefabBrowserKind.Buildable | PrefabBrowserKind.Prop | PrefabBrowserKind.Vegetation
            | PrefabBrowserKind.DroppedItem | PrefabBrowserKind.Creature | PrefabBrowserKind.Ragdoll | PrefabBrowserKind.Container | PrefabBrowserKind.Spawner;
        bool geologyRequiresTool = (kind & PrefabBrowserKind.Geology) == 0 || destructible && destructible!.m_minToolTier > 0;
        if (destructible && resourceDrops && (kind & resourceExclusions) == 0
            && geologyRequiresTool && destructible!.m_minToolTier > 0)
            kind |= PrefabBrowserKind.Mineable;
        if (wear && !buildable && !planting && !terrain
            && wear!.m_supports && (!piece || piece!.m_comfort == 0)
            && (kind & (PrefabBrowserKind.Container | PrefabBrowserKind.Vehicle | PrefabBrowserKind.Vegetation)) == 0)
            kind |= PrefabBrowserKind.Ruin;
        if (location && location!.m_applyRandomDamage) kind |= PrefabBrowserKind.Ruin;
        const PrefabBrowserKind propExclusions = PrefabBrowserKind.Container | PrefabBrowserKind.Vehicle | PrefabBrowserKind.Vegetation
            | PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable | PrefabBrowserKind.Ruin | PrefabBrowserKind.Creature
            | PrefabBrowserKind.Ragdoll | PrefabBrowserKind.Pickable | PrefabBrowserKind.PlayerDeath | PrefabBrowserKind.Location | PrefabBrowserKind.Dungeon;
        if (piece && !buildable && !planting && !terrain && (kind & propExclusions) == 0) kind |= PrefabBrowserKind.Prop;
        if (destructible && resourceDrops)
            kind |= PrefabBrowserKind.Resource;
        if (piece && piece!.m_category == Piece.PieceCategory.Misc && piece.m_comfort == 0
            && piece.m_resources is { Length: 1 } && piece.m_resources[0] is { } resource && resource.m_amount > 1
            && resource.m_resItem && IsMaterial(resource.m_resItem.gameObject)
            && wear && !wear!.m_supports && !Has<CraftingStation>() && !Has<StationExtension>()
            && !planting && !terrain && (kind & (PrefabBrowserKind.Container | PrefabBrowserKind.Vehicle | PrefabBrowserKind.BossTrigger)) == 0)
            kind |= PrefabBrowserKind.Stack;
        if (Has<LODGroup>() || Has<LodFadeInOut>() || Has<LightLod>() || Has<TerrainLod>()) kind |= PrefabBrowserKind.Lod;
        const PrefabBrowserKind persistent = PrefabBrowserKind.Buildable | PrefabBrowserKind.Prop | PrefabBrowserKind.DroppedItem
            | PrefabBrowserKind.Pickable | PrefabBrowserKind.Creature | PrefabBrowserKind.Ragdoll | PrefabBrowserKind.Vehicle
            | PrefabBrowserKind.Vegetation | PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable | PrefabBrowserKind.Ruin
            | PrefabBrowserKind.Container | PrefabBrowserKind.Location | PrefabBrowserKind.Dungeon | PrefabBrowserKind.Spawner;
        if ((kind & persistent) == 0 && !Has<Room>())
        {
            if (Has<ParticleSystem>() || Has<SmokeSpawner>() || Has<ParticleDecal>()) kind |= PrefabBrowserKind.Particle | PrefabBrowserKind.VisualFx;
            if (Has<EffectFade>() || Has<LightFlicker>() || Has<CamShaker>() || Has<AnimationEffect>()
                || Has<TimedDestruction>() && Has<Renderer>()) kind |= PrefabBrowserKind.VisualFx;
            if (Has<AudioSource>() || Has<ZSFX>()) kind |= PrefabBrowserKind.Audio;
        }
        if (!Has<Room>() && !location && (Has<MusicLocation>() || Has<MusicVolume>())) kind |= PrefabBrowserKind.Audio;
        return kind;
    }

    private IEnumerable<int> IndexReferences(GameObject source, Component[] components)
    {
        foreach (Component component in components)
        {
            yield return _names.Count;
            if (!source || !component) continue;
            IndexEffects(source, component);
            switch (component)
            {
                case ItemDrop item: IndexEffects(source, item.m_itemData.m_shared); break;
                case Plant plant:
                    foreach (GameObject grown in plant.m_grownPrefabs ?? []) Reference(source, grown, PrefabBrowserKind.Vegetation);
                    break;
                case TreeBase tree:
                    Reference(source, tree.m_stubPrefab, PrefabBrowserKind.Vegetation | PrefabBrowserKind.Destruction);
                    Reference(source, tree.m_logPrefab, PrefabBrowserKind.Vegetation | PrefabBrowserKind.Destruction);
                    Reference(source, tree.m_spawnOnDamage, PrefabBrowserKind.Destruction);
                    break;
                case TreeLog log: Reference(source, log.m_subLogPrefab, PrefabBrowserKind.Vegetation | PrefabBrowserKind.Destruction); break;
                case Destructible destructible:
                    Reference(source, destructible.m_spawnWhenDamaged, PrefabBrowserKind.Destruction);
                    Reference(source, destructible.m_spawnWhenDestroyed, PrefabBrowserKind.Destruction);
                    break;
                case WearNTear wear:
                    Reference(source, wear.m_worn, PrefabBrowserKind.Destruction);
                    Reference(source, wear.m_broken, PrefabBrowserKind.Destruction);
                    foreach (GameObject fragment in wear.m_fragmentRoots ?? [])
                        Reference(source, fragment, PrefabBrowserKind.Fracture | PrefabBrowserKind.Destruction);
                    break;
                case CreatureSpawner spawner: Reference(source, spawner.m_creaturePrefab, PrefabBrowserKind.Creature); break;
                case TriggerSpawner trigger:
                    foreach (GameObject creature in trigger.m_creaturePrefabs ?? []) Reference(source, creature, PrefabBrowserKind.Creature);
                    break;
                case EggGrow egg: Reference(source, egg.m_grownPrefab, PrefabBrowserKind.Creature); break;
                case SpawnAbility ability:
                    foreach (GameObject spawned in ability.m_spawnPrefab ?? []) Reference(source, spawned, PrefabBrowserKind.None);
                    break;
                case Feast feast:
                    foreach (Feast.FeastLevel level in feast.m_feastParts ?? [])
                    {
                        if (level is not { }) continue;
                        Reference(source, level.m_onAboveEquals, PrefabBrowserKind.Prop);
                        Reference(source, level.m_onBelow, PrefabBrowserKind.Prop);
                    }
                    break;
                case Location location:
                    Reference(source, location.m_interiorPrefab, PrefabBrowserKind.Location | PrefabBrowserKind.Dungeon);
                    if (location.m_generator) IndexDungeonTheme(location.m_generator);
                    foreach (int progress in IndexHierarchy(source, location.m_hasInterior)) yield return progress;
                    break;
                case Container container: Reference(source, container.m_destroyedLootPrefab, PrefabBrowserKind.Container); break;
                case OfferingBowl bowl when bowl.m_bossPrefab:
                    foreach (GameObject point in bowl.m_spawnPoints ?? []) Reference(source, point, PrefabBrowserKind.BossTrigger, false);
                    break;
            }
        }
    }

    private IEnumerable<int> IndexHierarchy(GameObject source, bool dungeon)
    {
        if (!source) yield break;
        foreach (ZNetView view in source.GetComponentsInChildren<ZNetView>(true))
        {
            yield return _names.Count;
            if (!view) continue;
            if (view.gameObject == source) continue;
            if (!_byName.TryGetValue(global::Utils.GetPrefabName(view.gameObject), out GameObject prefab)) continue;
            if (dungeon) Mark(prefab, PrefabBrowserKind.Dungeon);
            WearNTear? wear = prefab.GetComponent<WearNTear>();
            _evidence.TryGetValue(prefab, out PrefabBrowserKind kind);
            if (wear && wear!.m_supports && (kind & PrefabBrowserKind.Buildable) == 0) Mark(prefab, PrefabBrowserKind.Ruin);
        }
        if (!dungeon || !source) yield break;
        foreach (Collider collider in source.GetComponentsInChildren<Collider>(true))
        {
            yield return _names.Count;
            if (!collider) continue;
            if (!collider.GetComponentInParent<Room>()) continue;
            GameObject target = PingTargetResolver.GetPrefabRoot(collider);
            if (target.GetComponent<Room>()) continue;
            string name = PingTargetResolver.GetObjectId(target);
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (_byName.TryGetValue(name, out GameObject prefab)) Mark(prefab, PrefabBrowserKind.Dungeon);
            else
            {
                _names.Add(name);
                PrefabBrowserKind kind = PrefabBrowserKind.Dungeon
                    | (target.GetComponent<Pickable>() || target.GetComponent<PickableItem>()
                        ? PrefabBrowserKind.Pickable : PrefabBrowserKind.Prop);
                if (Components(target).OfType<DropOnDestroyed>().Any(drop => HasResourceDrops(drop.m_dropWhenDestroyed)))
                    kind |= PrefabBrowserKind.Resource;
                filter.Register(name, kind);
            }
        }
    }

    private void RegisterScenePickable(Component pickable)
    {
        string name = PingTargetResolver.GetObjectId(pickable.gameObject);
        if (string.IsNullOrWhiteSpace(name)) return;
        _names.Add(name);
        filter.Register(name, PrefabBrowserKind.Pickable
            | (pickable.GetComponentInParent<Room>() ? PrefabBrowserKind.Dungeon : PrefabBrowserKind.None));
    }

    private void IndexEffects(GameObject source, object data)
    {
        foreach (FieldInfo field in DataFields(data.GetType()))
        {
            if (!typeof(EffectList).IsAssignableFrom(field.FieldType) || field.GetValue(data) is not EffectList effects) continue;
            foreach (EffectList.EffectData effect in effects.m_effectPrefabs ?? [])
            {
                if (effect == null || !effect.m_prefab) continue;
                Component[] components = Components(effect.m_prefab);
                PrefabBrowserKind kind = PrefabBrowserKind.None;
                if (components.OfType<Renderer>().Any() || components.OfType<Light>().Any()
                    || components.OfType<CamShaker>().Any() || components.OfType<AnimationEffect>().Any()) kind |= PrefabBrowserKind.VisualFx;
                if (components.OfType<ParticleSystem>().Any()) kind |= PrefabBrowserKind.Particle;
                if (components.OfType<AudioSource>().Any() || components.OfType<ZSFX>().Any()) kind |= PrefabBrowserKind.Audio;
                Reference(source, effect.m_prefab, kind);
            }
        }
    }

    private IEnumerable<int> IndexLoadedRooms()
    {
        if (!DungeonDB.instance || AccessTools.Field(typeof(DungeonDB), "m_rooms")?.GetValue(DungeonDB.instance) is not IEnumerable rooms) yield break;
        foreach (object roomData in rooms)
        {
            yield return _names.Count;
            if (roomData == null) continue;
            foreach (FieldInfo field in DataFields(roomData.GetType()))
            {
                if (field.FieldType == typeof(Room) && field.GetValue(roomData) is Room room && room) Add(room.gameObject);
                else if (field.FieldType == typeof(SoftReference<GameObject>)
                    && field.GetValue(roomData) is SoftReference<GameObject> reference && reference.IsLoaded) Add(reference.Asset);
                else if (field.FieldType == typeof(SoftReference<Room>)
                    && field.GetValue(roomData) is SoftReference<Room> roomReference && roomReference.IsLoaded && roomReference.Asset)
                    Add(roomReference.Asset.gameObject);
            }
        }
    }

    private void IndexDungeonTheme(DungeonGenerator generator)
    {
        if (generator.m_algorithm == DungeonGenerator.Algorithm.Dungeon
            && AccessTools.Field(typeof(DungeonGenerator), "m_themes")?.GetValue(generator) is Room.Theme theme)
            _dungeonThemes |= theme;
    }

    private void Reference(GameObject source, GameObject? target, PrefabBrowserKind kind, bool add = true)
    {
        if (!target || target == source || target!.transform.IsChildOf(source.transform)) return;
        if (_byName.TryGetValue(global::Utils.GetPrefabName(target), out GameObject prefab)) target = prefab;
        else if (!add) return;
        Mark(target!, kind);
    }

    private void Add(GameObject? prefab)
    {
        if (!prefab || !_known.Add(prefab!)) return;
        _prefabs.Add(prefab!);
        _byName[prefab!.name] = prefab;
        _names.Add(prefab.name);
    }

    private void Mark(GameObject? prefab, PrefabBrowserKind kind)
    {
        if (!prefab) return;
        Add(prefab);
        _evidence.TryGetValue(prefab!, out PrefabBrowserKind existing);
        _evidence[prefab!] = existing | kind;
        filter.Register(prefab!.name, kind);
    }

    private Component[] Components(GameObject prefab)
    {
        if (_components.TryGetValue(prefab, out Component[] components)) return components;
        components = prefab.GetComponentsInChildren<Component>(true).Where(component => component && OwnedBy(prefab, component.transform)).ToArray();
        _components[prefab] = components;
        return components;
    }

    private static bool OwnedBy(GameObject prefab, Transform current)
    {
        while (current != prefab.transform)
        {
            if (current.GetComponent<ZNetView>()) return false;
            current = current.parent;
        }
        return true;
    }

    private IEnumerable<int> IndexResourceInputs()
    {
        foreach (GameObject prefab in _prefabs)
        {
            foreach (Component component in Components(prefab))
            {
                if (component is Smelter or CookingStation or Fermenter)
                    RegisterResourceInputs(component, "m_conversion", "m_from");
                else if (component is Incinerator)
                    RegisterNestedResourceInputs(component, "m_conversions", "m_requirements", "m_resItem");
            }
            yield return _names.Count;
        }
    }

    private void RegisterNestedResourceInputs(object source, string collectionFieldName,
        string requirementFieldName, string itemFieldName)
    {
        foreach (FieldInfo collectionField in DataFields(source.GetType()))
        {
            if (collectionField.Name != collectionFieldName
                || collectionField.GetValue(source) is not IEnumerable conversions) continue;
            foreach (object? conversion in conversions)
            {
                if (conversion == null) continue;
                FieldInfo? requirementsField = DataFields(conversion.GetType())
                    .FirstOrDefault(field => field.Name == requirementFieldName);
                if (requirementsField?.GetValue(conversion) is not IEnumerable requirements) continue;
                foreach (object? requirement in requirements)
                {
                    if (requirement == null) continue;
                    FieldInfo? itemField = DataFields(requirement.GetType())
                        .FirstOrDefault(field => field.Name == itemFieldName);
                    if (itemField?.GetValue(requirement) is ItemDrop item && item)
                        RegisterResourceInput(item.gameObject);
                }
            }
        }
    }

    private void RegisterResourceInputs(object source, string collectionFieldName, string itemFieldName)
    {
        foreach (FieldInfo collectionField in DataFields(source.GetType()))
        {
            if (collectionField.Name != collectionFieldName
                || collectionField.GetValue(source) is not IEnumerable entries) continue;
            foreach (object? entry in entries)
            {
                if (entry == null) continue;
                FieldInfo? itemField = DataFields(entry.GetType()).FirstOrDefault(field => field.Name == itemFieldName);
                if (itemField?.GetValue(entry) is ItemDrop item && item)
                    RegisterResourceInput(item.gameObject);
            }
        }
    }

    private void RegisterResourceInput(GameObject? item)
    {
        if (!item) return;
        string name = global::Utils.GetPrefabName(item!);
        if (!string.IsNullOrWhiteSpace(name)) _resourceInputs.Add(name);
    }

    private bool HasResourceDrops(DropTable? table) => table?.m_drops != null
        && table.m_drops.Any(drop => drop is { } && IsResourceInput(drop.m_item));

    private bool HasResourceDropPrefab(GameObject? prefab)
    {
        if (!prefab) return false;
        Component[] components = Components(prefab!);
        return components.OfType<ItemDrop>().Any(item => IsResourceInput(item.gameObject))
            || components.OfType<Pickable>().Any(pickable => IsResourceInput(pickable.m_itemPrefab))
            || components.OfType<MineRock>().Any(mineRock => HasResourceDrops(mineRock.m_dropItems))
            || components.OfType<MineRock5>().Any(mineRock => HasResourceDrops(mineRock.m_dropItems));
    }

    private bool IsResourceInput(GameObject? item)
    {
        if (!item) return false;
        string name = global::Utils.GetPrefabName(item!);
        return !EnvironmentalResourceExclusions.Contains(name)
            && (_resourceInputs.Contains(name) || IsMaterial(item!));
    }

    private static bool IsMaterial(GameObject prefab) => prefab.GetComponent<ItemDrop>() is { } item
        && item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material;

    private static FieldInfo[] DataFields(Type type)
    {
        if (Fields.TryGetValue(type, out FieldInfo[] fields)) return fields;
        List<FieldInfo> result = [];
        for (Type? current = type; current != null && current != typeof(UnityEngine.Object); current = current.BaseType)
            result.AddRange(current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        fields = result.ToArray();
        Fields[type] = fields;
        return fields;
    }
}