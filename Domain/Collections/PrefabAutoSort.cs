using System.Collections.Generic;
using System.Linq;
using JoksterCube.PingMark.Domain.Enums;
using JoksterCube.PingMark.Domain.Models;
using YamlDotNet.Serialization;
using TargetLists = JoksterCube.PingMark.Settings.Constants.Groups.PingTargets;

namespace JoksterCube.PingMark.Domain.Collections;

internal static class PrefabAutoSort
{
    // Auto sort rebuilds the default sections, so names and colors come from the default collections.
    private static readonly SerializedSections DefaultSections = new DeserializerBuilder().Build()
        .Deserialize<SerializedSections>(TargetLists.PrefabSections.DefaultValue);
    internal static readonly string[] SectionNames = DefaultSections.Sections.Select(section => section.Name).ToArray();
    private static readonly int[] SectionColorIds = DefaultSections.Sections.Select(section => section.ColorId).ToArray();
    private static readonly string[] AdditionalUsefulPrefabNames =
    [
        "RuneStone_Memorial1", "Vegvisir_DNBoss", "offferaltar_memorialsite",
        "RuneTablet_Fader", "Offeraltar_fader", "fader_bellholder"
    ];
    private static readonly Dictionary<string, int> Defaults = BuildDefaults();

    internal static List<PrefabCollectionSection> CreateSections(IEnumerable<string> names, PrefabBrowserFilter filter)
    {
        List<PrefabCollectionSection> sections = SectionNames.Select((name, index) => new PrefabCollectionSection
        {
            Name = name,
            ColorId = SectionColorIds[index]
        }).ToList();
        List<string>[] sorted = SectionNames.Select(_ => new List<string>()).ToArray();
        foreach (string name in names.Distinct(System.StringComparer.OrdinalIgnoreCase))
        {
            int category = Category(name, filter.Kind(name));
            if (category >= 0) sorted[category].Add(name);
        }
        for (int index = 0; index < sections.Count; index++)
            sections[index].Prefabs = string.Join(", ", sorted[index].OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase));
        return sections;
    }

    // Indexes into the default sections; -1 leaves the prefab Unassigned.
    private const int Unassigned = -1, Valuables = 0, Locations = 1, Useful = 2;

    private const PrefabBrowserKind NeverPinned = PrefabBrowserKind.DroppedItem | PrefabBrowserKind.PlayerDeath
        | PrefabBrowserKind.Ragdoll | PrefabBrowserKind.Tree | PrefabBrowserKind.DungeonRoom;

    // Vanilla prefabs the rules would misplace: an environment helper registered like a location, spreading fires
    // that look like campfires (fire component and cinder spawner) but are hazards, developer test locations that
    // never spawn, and plain bushes whose few drops would outline every forest bush as a valuable.
    private static readonly HashSet<string> ExcludedPrefabNames = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "InteriorEnvironmentZoneForce", "Fire", "HouseFire",
        "DevBedchamber", "DevCombatRange", "DevCombatRing", "DevDressingRoom", "DevFloor1", "DevForge", "DevGarden",
        "DevGround1", "DevGround2", "DevHouse1", "DevHouse2", "DevHouse3", "DevHouse4", "DevHouse5", "DevHouseStart",
        "DevKitchen", "DevMageRoom", "DevSoundTest", "DevWall1", "DevWall2", "DevWallAsh", "Hugintest",
        "Rock_destructible_test", "Bush01", "Bush01_deepnorth", "Bush01_heath", "shrub_2", "shrub_2_heath"
    };

    private const PrefabBrowserKind NotWorldTargets = PrefabBrowserKind.BuildingPiece | PrefabBrowserKind.Buildable
        | PrefabBrowserKind.Planting | PrefabBrowserKind.Craftable | PrefabBrowserKind.Projectile | PrefabBrowserKind.Particle
        | PrefabBrowserKind.VisualFx | PrefabBrowserKind.Audio | PrefabBrowserKind.SystemController
        | PrefabBrowserKind.Development | PrefabBrowserKind.Cinematic | PrefabBrowserKind.Creature
        | PrefabBrowserKind.AmbientCreature | PrefabBrowserKind.TerrainTool;

    internal static int Category(string name, PrefabBrowserKind kind) => Category(name, kind, true);

    // The rules alone, without the curated vanilla list: what modded prefabs get.
    internal static int HeuristicCategory(string name, PrefabBrowserKind kind) => Category(name, kind, false);

    internal static int DefaultCategory(string name) => Defaults.TryGetValue(name, out int category) ? category : Unassigned;

    internal static string SectionName(int category) =>
        category >= 0 && category < SectionNames.Length ? SectionNames[category] : "Unassigned";

    private const PrefabBrowserKind ValuableBeyondDrops = PrefabBrowserKind.Container
        | PrefabBrowserKind.Useful | PrefabBrowserKind.Spawner | PrefabBrowserKind.Location | PrefabBrowserKind.Portal
        | PrefabBrowserKind.Vehicle | PrefabBrowserKind.BossTrigger | PrefabBrowserKind.TriggerPoint;

    // Anything that marks a prefab as a lasting world object rather than a short-lived effect.
    private const PrefabBrowserKind WorldObject = PrefabBrowserKind.Buildable | PrefabBrowserKind.BuildingPiece
        | PrefabBrowserKind.Location | PrefabBrowserKind.Resource | PrefabBrowserKind.Container | PrefabBrowserKind.Pickable
        | PrefabBrowserKind.Mineable | PrefabBrowserKind.Geology | PrefabBrowserKind.Vegetation | PrefabBrowserKind.Prop
        | PrefabBrowserKind.Ruin | PrefabBrowserKind.Dungeon | PrefabBrowserKind.Vehicle | PrefabBrowserKind.Portal;

    // The curated vanilla list decides first; the rules below cover everything it does not know, such as modded
    // prefabs. Rules run in priority order and the first match decides, so a player-built chest
    // (Buildable | Container) is a Valuable while a plain building piece stays Unassigned.
    private static int Category(string name, PrefabBrowserKind kind, bool useDefaults)
    {
        if (Any(kind, NeverPinned) || ExcludedPrefabNames.Contains(name)) return Unassigned;
        // Anything that can be picked up is worth finding, whatever it gives and whatever else it is.
        // Player-planted crops still growing are the exception.
        if (Any(kind, PrefabBrowserKind.Pickable) && !Any(kind, PrefabBrowserKind.Planting)) return Valuables;
        // Rocks, roots and debris that only ever drop wood, stone, grausten or ice are not worth a ping, even when
        // listed in the curated defaults; chests, pickables, stations and sites keep their own reasons.
        if (Any(kind, PrefabBrowserKind.CommonDropsOnly) && !Any(kind, ValuableBeyondDrops)) return Unassigned;
        if (useDefaults && Defaults.TryGetValue(name, out int category)) return category;
        // Boss altar locations share names with their bosses ("Bonemass"), so locations are decided before creatures.
        // Single-object "locations" (an ore arch, a runestone) belong with what they are.
        if (Any(kind, PrefabBrowserKind.Location))
        {
            if (Any(kind, PrefabBrowserKind.Mineable)) return Valuables;
            if (Any(kind, PrefabBrowserKind.Useful) && !Any(kind, PrefabBrowserKind.Dungeon)) return Useful;
            return Locations;
        }
        // Creatures that summon or spawn (Troll_Summoned, modded summoners) are still creatures; pinging them
        // highlights the creature instead of using a section anyway. Birds are destructible and stay below.
        if (Any(kind, PrefabBrowserKind.Creature)) return Unassigned;
        // Fireballs and burning projectiles count as spawners because they leave fire behind.
        if (Any(kind, PrefabBrowserKind.Projectile) && Any(kind, PrefabBrowserKind.Spawner) && !Any(kind, WorldObject))
            return Unassigned;
        if (Any(kind, PrefabBrowserKind.Spawner)) return Useful;
        if (Any(kind, PrefabBrowserKind.Useful | PrefabBrowserKind.Vehicle | PrefabBrowserKind.Portal)) return Useful;
        if (Any(kind, PrefabBrowserKind.Resource | PrefabBrowserKind.Container)
            || Any(kind, PrefabBrowserKind.Pickable) && !Any(kind, PrefabBrowserKind.Planting)) return Valuables;
        if (Any(kind, NotWorldTargets)) return Unassigned;
        if (Any(kind, PrefabBrowserKind.BossTrigger | PrefabBrowserKind.TriggerPoint)) return Useful;
        if (Any(kind, PrefabBrowserKind.Stack | PrefabBrowserKind.Mineable)) return Valuables;
        // Dungeon-only parts (room walls, floors, decor) stay Unassigned: listing them makes every surface pingable.
        if (Any(kind, PrefabBrowserKind.Ruin)) return Locations;
        return Unassigned;
    }

    private static bool Any(PrefabBrowserKind kind, PrefabBrowserKind flags) => (kind & flags) != 0;

    private static Dictionary<string, int> BuildDefaults()
    {
        SerializedSections defaults = DefaultSections;
        Dictionary<string, int> categories = new(System.StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < defaults.Sections.Count; index++)
            foreach (string name in defaults.Sections[index].Prefabs.Split(','))
                if (!string.IsNullOrWhiteSpace(name)) categories[name.Trim()] = index;
        foreach (string name in AdditionalUsefulPrefabNames)
            categories[name] = 2;
        return categories;
    }
}