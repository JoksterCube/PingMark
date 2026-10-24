using System.Linq;
using JoksterCube.PingMark.Domain;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain.Enums;
using JoksterCube.PingMark.Domain.Models;
using JoksterCube.PingMark.Domain.Networking;
using UnityEngine;
using Xunit;

namespace JoksterCube.PingMark.Tests;

public class TargetNameFormatterTests
{
    [Theory]
    [InlineData("MineRock_Copper2", "Mine Rock Copper")]
    [InlineData("TreasureChest_meadows", "Treasure Chest meadows")]
    [InlineData("Crypt 4", "Crypt 4")]
    [InlineData("  Burial   Chambers  ", "Burial Chambers")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Formats_prefab_ids_and_keeps_display_names(string? input, string expected) =>
        Assert.Equal(expected, TargetNameFormatter.Format(input));
}

public class RangeTests
{
    [Fact]
    public void Zero_range_is_unlimited() =>
        Assert.True(ZPingNetwork.IsInRange(Vector3.zero, new Vector3(50000f, 0f, 0f), 0f));

    [Fact]
    public void Negative_range_never_matches() =>
        Assert.False(ZPingNetwork.IsInRange(Vector3.zero, Vector3.zero, -1f));

    [Theory]
    [InlineData(199.9f, true)]
    [InlineData(200f, true)]
    [InlineData(200.1f, false)]
    public void Range_is_inclusive(float distance, bool expected) =>
        Assert.Equal(expected, ZPingNetwork.IsInRange(Vector3.zero, new Vector3(0f, 0f, distance), 200f));
}

public class PingRateLimiterTests
{
    [Fact]
    public void Allows_a_burst_then_blocks_and_refills()
    {
        const long sender = 4242;
        PingRateLimiter.Forget(sender);
        for (int i = 0; i < 10; i++) Assert.True(PingRateLimiter.TryConsume(sender, 100f));
        Assert.False(PingRateLimiter.TryConsume(sender, 100f));
        Assert.True(PingRateLimiter.TryConsume(sender, 100.3f));
        Assert.False(PingRateLimiter.TryConsume(sender, 100.3f));
    }

    [Fact]
    public void Senders_have_separate_budgets()
    {
        PingRateLimiter.Forget(1);
        PingRateLimiter.Forget(2);
        for (int i = 0; i < 10; i++) PingRateLimiter.TryConsume(1, 0f);
        Assert.False(PingRateLimiter.TryConsume(1, 0f));
        Assert.True(PingRateLimiter.TryConsume(2, 0f));
    }
}

public class PrefabAutoSortTests
{
    // Kinds as a vanilla scan reports them, run through the same entry point as the editor's Auto Sort button.
    [Fact]
    public void Auto_sort_places_reported_vanilla_and_modded_prefabs()
    {
        var kinds = new (string Name, PrefabBrowserKind Kind)[]
        {
            ("Crow", PrefabBrowserKind.AmbientCreature | PrefabBrowserKind.Lod | PrefabBrowserKind.Resource),
            ("bow_projectile_fire", PrefabBrowserKind.Spawner | PrefabBrowserKind.Projectile | PrefabBrowserKind.Useful),
            ("Fire", PrefabBrowserKind.Spawner | PrefabBrowserKind.Lod | PrefabBrowserKind.Useful),
            ("Troll_Summoned", PrefabBrowserKind.Creature | PrefabBrowserKind.Spawner | PrefabBrowserKind.Useful),
            ("DevHouse1", PrefabBrowserKind.Location),
            ("Hugintest", PrefabBrowserKind.Location),
            ("InteriorEnvironmentZoneForce", PrefabBrowserKind.Location),
            ("DG_Cave", PrefabBrowserKind.Dungeon),
            ("Bush01", PrefabBrowserKind.Lod | PrefabBrowserKind.Vegetation | PrefabBrowserKind.Resource),
            ("MineRock_Stone", PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable | PrefabBrowserKind.CommonDropsOnly),
            ("MineRock_Tin", PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable),
            ("Crypt2", PrefabBrowserKind.Location | PrefabBrowserKind.Dungeon),
            ("Greydwarf_camp1", PrefabBrowserKind.Location),
            ("Bonemass", PrefabBrowserKind.Creature | PrefabBrowserKind.Location),
            ("incinerator", PrefabBrowserKind.Buildable | PrefabBrowserKind.Container | PrefabBrowserKind.Useful),
            ("Beehive", PrefabBrowserKind.Projectile | PrefabBrowserKind.Ruin | PrefabBrowserKind.BuildingPiece),
            ("forestcrypt_new_BurialChamber02", PrefabBrowserKind.DungeonRoom | PrefabBrowserKind.Dungeon | PrefabBrowserKind.Useful),
            ("ModdedLootChest", PrefabBrowserKind.Container),
            ("ModdedStoneRock", PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable | PrefabBrowserKind.CommonDropsOnly),
            ("ModdedVillage", PrefabBrowserKind.Location),
        };
        PrefabBrowserFilter filter = new();
        foreach (var (name, kind) in kinds) filter.Register(name, kind);

        var sections = PrefabAutoSort.CreateSections(kinds.Select(entry => entry.Name), filter);
        string? SectionOf(string name) => sections.FirstOrDefault(section => section.Contains(name))?.Name;

        Assert.Equal("Valuables", SectionOf("Crow"));
        Assert.Equal("Valuables", SectionOf("MineRock_Tin"));
        Assert.Equal("Valuables", SectionOf("Beehive"));
        Assert.Equal("Valuables", SectionOf("ModdedLootChest"));
        Assert.Equal("Locations", SectionOf("Crypt2"));
        Assert.Equal("Locations", SectionOf("Greydwarf_camp1"));
        Assert.Equal("Locations", SectionOf("ModdedVillage"));
        Assert.Equal("Useful", SectionOf("Bonemass"));
        Assert.Equal("Useful", SectionOf("incinerator"));
        foreach (string unassigned in new[] { "bow_projectile_fire", "Fire", "Troll_Summoned", "DevHouse1", "Hugintest",
                     "InteriorEnvironmentZoneForce", "DG_Cave", "Bush01", "MineRock_Stone", "forestcrypt_new_BurialChamber02",
                     "ModdedStoneRock" })
            Assert.Null(SectionOf(unassigned));
    }

    [Theory]
    [InlineData((ulong)(PrefabBrowserKind.DroppedItem | PrefabBrowserKind.Resource), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Location), 1)]
    [InlineData((ulong)(PrefabBrowserKind.Spawner), 2)]
    [InlineData((ulong)(PrefabBrowserKind.Container), 0)]
    [InlineData((ulong)(PrefabBrowserKind.Creature), -1)]
    [InlineData((ulong)(PrefabBrowserKind.None), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable), 0)]
    [InlineData((ulong)(PrefabBrowserKind.Buildable | PrefabBrowserKind.Container), 0)]
    [InlineData((ulong)(PrefabBrowserKind.Buildable | PrefabBrowserKind.BuildingPiece), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Pickable | PrefabBrowserKind.Planting), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Tree | PrefabBrowserKind.Resource), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Spawner | PrefabBrowserKind.Container), 2)]
    [InlineData((ulong)(PrefabBrowserKind.BossTrigger), 2)]
    [InlineData((ulong)(PrefabBrowserKind.Creature | PrefabBrowserKind.BossTrigger), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Ruin), 1)]
    [InlineData((ulong)(PrefabBrowserKind.Dungeon | PrefabBrowserKind.Prop), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Dungeon | PrefabBrowserKind.Container), 0)]
    [InlineData((ulong)(PrefabBrowserKind.DungeonRoom | PrefabBrowserKind.Dungeon | PrefabBrowserKind.Useful), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Creature | PrefabBrowserKind.Spawner), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Location | PrefabBrowserKind.Creature), 1)]
    // Cases from a vanilla auto sort report: fire projectiles, birds, ore and runestone "locations", campfires.
    [InlineData((ulong)(PrefabBrowserKind.Spawner | PrefabBrowserKind.Projectile | PrefabBrowserKind.Useful), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Projectile | PrefabBrowserKind.Useful), 2)]
    [InlineData((ulong)(PrefabBrowserKind.AmbientCreature | PrefabBrowserKind.Resource), 0)]
    [InlineData((ulong)(PrefabBrowserKind.Location | PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable), 0)]
    [InlineData((ulong)(PrefabBrowserKind.Location | PrefabBrowserKind.Useful), 2)]
    [InlineData((ulong)(PrefabBrowserKind.Location | PrefabBrowserKind.Dungeon | PrefabBrowserKind.Useful), 1)]
    [InlineData((ulong)(PrefabBrowserKind.Buildable | PrefabBrowserKind.Spawner | PrefabBrowserKind.Projectile
        | PrefabBrowserKind.Useful | PrefabBrowserKind.BuildingPiece), 2)]
    [InlineData((ulong)(PrefabBrowserKind.Spawner | PrefabBrowserKind.Projectile | PrefabBrowserKind.Useful
        | PrefabBrowserKind.Resource), 2)]
    // The enum is internal, so theory data passes its raw value.
    public void Categorizes_by_kind(ulong kind, int expected) =>
        Assert.Equal(expected, PrefabAutoSort.Category("SomeUnknownModdedPrefab", (PrefabBrowserKind)kind));

    [Fact]
    public void Known_default_names_keep_their_section()
    {
        Assert.Equal(2, PrefabAutoSort.Category("portal_wood", PrefabBrowserKind.None));
        Assert.Equal(0, PrefabAutoSort.Category("MineRock_Tin", PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable));
    }

    [Fact]
    public void Heuristic_category_ignores_the_curated_defaults()
    {
        Assert.Equal(2, PrefabAutoSort.DefaultCategory("portal_wood"));
        Assert.Equal(-1, PrefabAutoSort.HeuristicCategory("portal_wood", PrefabBrowserKind.None));
        Assert.Equal(2, PrefabAutoSort.HeuristicCategory("portal_wood", PrefabBrowserKind.Portal));
    }

    [Fact]
    public void Common_material_drops_are_not_valuables_even_when_curated()
    {
        const PrefabBrowserKind stoneRock = PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable | PrefabBrowserKind.CommonDropsOnly;
        Assert.Equal(-1, PrefabAutoSort.Category("MineRock_Stone", stoneRock));
        Assert.Equal(-1, PrefabAutoSort.Category("SomeModdedStoneRock", stoneRock));
        Assert.Equal(0, PrefabAutoSort.Category("MineRock_Tin", PrefabBrowserKind.Geology | PrefabBrowserKind.Mineable));
        Assert.Equal(0, PrefabAutoSort.Category("SomeModdedCrate",
            PrefabBrowserKind.Container | PrefabBrowserKind.CommonDropsOnly));
        // Pickables outrank everything else, including common drops and the curated list.
        Assert.Equal(0, PrefabAutoSort.Category("BlueberryBush", PrefabBrowserKind.Pickable | PrefabBrowserKind.CommonDropsOnly));
        Assert.Equal(0, PrefabAutoSort.Category("portal_wood", PrefabBrowserKind.Pickable));
        Assert.Equal(0, PrefabAutoSort.Category("ModdedPickableShrine", PrefabBrowserKind.Pickable | PrefabBrowserKind.Useful
            | PrefabBrowserKind.Location));
        Assert.Equal(-1, PrefabAutoSort.Category("SaplingCarrot", PrefabBrowserKind.Pickable | PrefabBrowserKind.Planting));
    }

    [Fact]
    public void Environment_helpers_registered_as_locations_stay_unassigned() =>
        Assert.Equal(-1, PrefabAutoSort.Category("InteriorEnvironmentZoneForce", PrefabBrowserKind.Location));

    [Fact]
    public void Section_names_come_from_the_defaults() =>
        Assert.Equal(["Valuables", "Locations", "Useful"], PrefabAutoSort.SectionNames);
}

public class HighlightRegistryTests
{
    private static HighlightRegistry<int, string> Registry() => new();

    [Fact]
    public void Expired_highlights_are_dropped()
    {
        var registry = Registry();
        registry.Add(sender: 1, id: 10, "Ann", isPlayer: false, expiration: 5f);
        registry.Trim(now: 4f, limitPerSender: 1, mobsEnabled: true, playersEnabled: true);
        Assert.True(registry.IsActive(10));
        registry.Trim(now: 5f, limitPerSender: 1, mobsEnabled: true, playersEnabled: true);
        Assert.False(registry.IsActive(10));
    }

    [Fact]
    public void Each_sender_keeps_only_their_newest_highlights()
    {
        var registry = Registry();
        registry.Add(1, 10, "Ann", false, 100f);
        registry.Add(2, 20, "Bob", false, 100f);
        registry.Add(1, 11, "Ann", false, 100f);
        registry.Trim(0f, 1, true, true);

        Assert.False(registry.IsActive(10));
        Assert.True(registry.IsActive(11));
        Assert.True(registry.IsActive(20));
    }

    [Fact]
    public void The_newest_highlight_decides_the_pinger()
    {
        var registry = Registry();
        registry.Add(1, 10, "Ann", false, 100f);
        registry.Add(2, 10, "Bob", false, 100f);
        registry.Trim(0f, 1, true, true);

        Assert.True(registry.TryGetActive(10, out var entry));
        Assert.Equal("Bob", entry.User);
    }

    [Fact]
    public void Disabled_kinds_are_removed()
    {
        var registry = Registry();
        registry.Add(1, 10, "Ann", isPlayer: true, 100f);
        registry.Add(1, 11, "Ann", isPlayer: false, 100f);
        registry.Trim(0f, 5, mobsEnabled: true, playersEnabled: false);

        Assert.False(registry.IsActive(10));
        Assert.True(registry.IsActive(11));
    }
    [Fact]
    public void Unlimited_highlights_bypass_the_sender_limit()
    {
        var registry = Registry();
        registry.Add(1, 10, "Ann", false, 100f, unlimited: true);
        registry.Add(1, 11, "Ann", false, 100f, unlimited: true);
        registry.Add(1, 12, "Ann", false, 100f);
        registry.Add(1, 13, "Ann", false, 100f);
        registry.Trim(0f, 1, true, true);

        Assert.True(registry.IsActive(10));
        Assert.True(registry.IsActive(11));
        Assert.False(registry.IsActive(12));
        Assert.True(registry.IsActive(13));
    }
}
