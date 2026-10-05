using System.Collections.Generic;
using System.Linq;
using JoksterCube.PingMark.Domain.Enums;
using JoksterCube.PingMark.Domain.Models;
using YamlDotNet.Serialization;
using TargetLists = JoksterCube.PingMark.Settings.Constants.Groups.PingTargets;

namespace JoksterCube.PingMark.Domain.Collections;

internal static class PrefabAutoSort
{
    internal static readonly string[] SectionNames = ["Valuables", "Locations", "Useful"];
    private static readonly int[] SectionColorIds = [5, 9, 8];
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
        List<string>[] sorted = [[], [], []];
        foreach (string name in names.Distinct(System.StringComparer.OrdinalIgnoreCase))
        {
            int category = Category(name, filter.Kind(name));
            if (category >= 0) sorted[category].Add(name);
        }
        for (int index = 0; index < sections.Count; index++)
            sections[index].Prefabs = string.Join(", ", sorted[index].OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase));
        return sections;
    }

    internal static int Category(string name, PrefabBrowserKind kind)
    {
        if ((kind & (PrefabBrowserKind.DroppedItem | PrefabBrowserKind.PlayerDeath
            | PrefabBrowserKind.Ragdoll | PrefabBrowserKind.Tree)) != 0) return -1;
        if ((kind & PrefabBrowserKind.Location) != 0)
            return Defaults.TryGetValue(name, out int locationCategory) ? locationCategory : 1;
        if ((kind & PrefabBrowserKind.Spawner) != 0) return 2;
        if (Defaults.TryGetValue(name, out int category)) return category;
        if ((kind & PrefabBrowserKind.Useful) != 0) return 2;
        if ((kind & (PrefabBrowserKind.Vehicle | PrefabBrowserKind.Portal)) != 0) return 2;
        if ((kind & PrefabBrowserKind.Resource) != 0) return 0;
        if ((kind & PrefabBrowserKind.Container) != 0) return 0;
        if ((kind & PrefabBrowserKind.Pickable) != 0 && (kind & PrefabBrowserKind.Planting) == 0) return 0;
        if ((kind & (PrefabBrowserKind.BuildingPiece | PrefabBrowserKind.Buildable
            | PrefabBrowserKind.Planting | PrefabBrowserKind.Craftable)) != 0) return -1;
        const PrefabBrowserKind excluded = PrefabBrowserKind.Projectile | PrefabBrowserKind.Particle
            | PrefabBrowserKind.VisualFx | PrefabBrowserKind.Audio | PrefabBrowserKind.SystemController
            | PrefabBrowserKind.Development | PrefabBrowserKind.Cinematic | PrefabBrowserKind.Ragdoll
            | PrefabBrowserKind.PlayerDeath | PrefabBrowserKind.DroppedItem;
        if ((kind & excluded) != 0) return -1;
        if ((kind & (PrefabBrowserKind.Creature | PrefabBrowserKind.AmbientCreature | PrefabBrowserKind.TerrainTool)) != 0)
            return -1;
        if ((kind & (PrefabBrowserKind.BossTrigger | PrefabBrowserKind.TriggerPoint)) != 0
            || (kind & PrefabBrowserKind.Spawner) != 0 && (kind & PrefabBrowserKind.Container) == 0) return 2;
        if ((kind & (PrefabBrowserKind.Stack | PrefabBrowserKind.Pickable | PrefabBrowserKind.Mineable)) != 0
            || (kind & PrefabBrowserKind.Resource) != 0 && (kind & PrefabBrowserKind.DroppedItem) == 0) return 0;
        if ((kind & (PrefabBrowserKind.Buildable | PrefabBrowserKind.Craftable)) != 0) return -1;
        if ((kind & (PrefabBrowserKind.Pickable | PrefabBrowserKind.Mineable | PrefabBrowserKind.Container
            | PrefabBrowserKind.Resource | PrefabBrowserKind.Stack)) != 0) return 0;
        if ((kind & (PrefabBrowserKind.Location | PrefabBrowserKind.Dungeon | PrefabBrowserKind.Ruin)) != 0) return 1;
        return -1;
    }

    private static Dictionary<string, int> BuildDefaults()
    {
        SerializedSections defaults = new DeserializerBuilder().Build()
            .Deserialize<SerializedSections>(TargetLists.PrefabSections.DefaultValue);
        Dictionary<string, int> categories = new(System.StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < defaults.Sections.Count; index++)
            foreach (string name in defaults.Sections[index].Prefabs.Split(','))
                if (!string.IsNullOrWhiteSpace(name)) categories[name.Trim()] = index;
        foreach (string name in AdditionalUsefulPrefabNames)
            categories[name] = 2;
        return categories;
    }
}