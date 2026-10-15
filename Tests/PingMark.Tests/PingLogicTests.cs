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
    [Theory]
    [InlineData((ulong)(PrefabBrowserKind.DroppedItem | PrefabBrowserKind.Resource), -1)]
    [InlineData((ulong)(PrefabBrowserKind.Location), 1)]
    [InlineData((ulong)(PrefabBrowserKind.Spawner), 2)]
    [InlineData((ulong)(PrefabBrowserKind.Container), 0)]
    [InlineData((ulong)(PrefabBrowserKind.Creature), -1)]
    [InlineData((ulong)(PrefabBrowserKind.None), -1)]
    // The enum is internal, so theory data passes its raw value.
    public void Categorizes_by_kind(ulong kind, int expected) =>
        Assert.Equal(expected, PrefabAutoSort.Category("SomeUnknownModdedPrefab", (PrefabBrowserKind)kind));

    [Fact]
    public void Known_default_names_keep_their_section() =>
        Assert.Equal(2, PrefabAutoSort.Category("portal_wood", PrefabBrowserKind.None));

    [Fact]
    public void Section_names_come_from_the_defaults() =>
        Assert.Equal(["Valuables", "Locations", "Useful"], PrefabAutoSort.SectionNames);
}
