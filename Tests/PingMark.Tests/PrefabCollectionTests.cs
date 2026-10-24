using System.Collections.Generic;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain.Models;
using Xunit;

namespace JoksterCube.PingMark.Tests;

public class PrefabCollectionsTests
{
    private static PrefabCollections Create() => new(
        ["a", "b", "c", "d"],
        new List<PrefabCollectionSection>
        {
            new() { Name = "First", Prefabs = "a, b" },
            new() { Name = "Second", Prefabs = "b, c" }
        });

    [Fact]
    public void Unlisted_prefabs_are_unassigned_and_later_sections_win_duplicates()
    {
        PrefabCollections collections = Create();
        Assert.Equal(["d"], collections.Names(0, ""));
        Assert.Equal(["a"], collections.Names(1, ""));
        Assert.Equal(["b", "c"], collections.Names(2, ""));
    }

    [Fact]
    public void Move_and_undo_restore_the_original_layout()
    {
        PrefabCollections collections = Create();
        collections.Move(["A", "d"], 2);
        Assert.Equal(["a", "b", "c", "d"], collections.Names(2, ""));

        Assert.True(collections.Undo());
        Assert.Equal(["d"], collections.Names(0, ""));
        Assert.Equal(["a"], collections.Names(1, ""));
        Assert.False(collections.CanUndo);
    }

    [Fact]
    public void Removing_a_section_returns_its_prefabs_to_unassigned()
    {
        PrefabCollections collections = Create();
        collections.RemoveSection(1);
        Assert.Equal(2, collections.CollectionCount);
        Assert.Equal(["a", "d"], collections.Names(0, ""));
    }

    [Fact]
    public void Replacing_a_section_moves_listed_names_in_and_returns_the_rest()
    {
        PrefabCollections collections = Create();
        collections.Move(["d"], 1);
        collections.Replace(1, "C, d, modded_prefab");

        Assert.Equal(["a"], collections.Names(0, ""));
        Assert.Equal(["c", "d", "modded_prefab"], collections.Names(1, ""));
        Assert.Equal(["b"], collections.Names(2, ""));
        Assert.False(collections.CanUndo);
    }

    [Fact]
    public void Clone_is_independent_and_keeps_undo_history()
    {
        PrefabCollections original = Create();
        original.Move(["d"], 1);
        PrefabCollections clone = original.Clone();
        original.Move(["c"], 1);

        Assert.Equal(["a", "d"], clone.Names(1, ""));
        Assert.True(clone.Undo());
        Assert.Equal(["a"], clone.Names(1, ""));
    }

    [Fact]
    public void Search_is_case_insensitive() =>
        Assert.Equal(["b"], Create().Names(2, "B"));
}

public class PrefabCollectionSectionConfigTests
{
    [Fact]
    public void Serialized_sections_round_trip()
    {
        List<PrefabCollectionSection> sections =
        [
            new() { Name = " Loot ", Prefabs = "a, b", Enabled = false, ColorId = 3 },
            new() { Name = "Places", Prefabs = "c", ColorId = 9 }
        ];

        List<PrefabCollectionSection>? parsed = PrefabCollectionSectionConfig.Parse(PrefabCollectionSectionConfig.Serialize(sections));

        Assert.NotNull(parsed);
        Assert.Equal(["Loot", "Places"], parsed!.ConvertAll(section => section.Name));
        Assert.False(parsed[0].Enabled);
        Assert.Equal(9, parsed[1].ColorId);
        Assert.True(parsed[0].Contains("B"));
    }

    [Fact]
    public void Invalid_yaml_is_rejected_instead_of_replaced() =>
        Assert.Null(PrefabCollectionSectionConfig.Parse("Sections: [ this is : not valid"));

    [Fact]
    public void Out_of_range_color_ids_are_clamped()
    {
        List<PrefabCollectionSection>? parsed = PrefabCollectionSectionConfig.Parse("Sections:\n- Name: X\n  ColorId: 99\n");
        Assert.Equal(12, Assert.Single(parsed!).ColorId);
    }
}
