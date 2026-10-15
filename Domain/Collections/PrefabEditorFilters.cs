using JoksterCube.PingMark.Domain.Enums;
using UnityEngine;
using static JoksterCube.PingMark.Domain.Collections.PrefabCollectionEditorUI;
using static JoksterCube.PingMark.Domain.Collections.PrefabEditorStyles;

namespace JoksterCube.PingMark.Domain.Collections;

// The Include / Hide / Only category filter panel of the Prefab Collections editor.
internal static class PrefabEditorFilters
{
    private static readonly (PrefabBrowserKind Kind, string Text, string Tooltip)[] FilterOptions =
    [
        (PrefabBrowserKind.Buildable, "Buildable", "Prefabs registered in player build tables"),
        (PrefabBrowserKind.Craftable, "Craftable", "Items produced by recipes in ObjectDB"),
        (PrefabBrowserKind.Pickable, "Pickable", "World pickups such as plants, ore piles and treasures"),
        (PrefabBrowserKind.Location, "Locations", "Whole world location prefabs from ZoneSystem and Location components"),
        (PrefabBrowserKind.Dungeon, "Dungeons", "Interior locations, true dungeon generators and assets used by loaded dungeon rooms"),
        (PrefabBrowserKind.DroppedItem, "Dropped items", "ItemDrop prefabs, including materials and equipment"),
        (PrefabBrowserKind.Prop, "Props", "World pieces, item stands, feast furniture and referenced feast visuals"),
        (PrefabBrowserKind.Stack, "Stacks", "Non-supporting bulk material pieces with a single material resource cost"),
        (PrefabBrowserKind.Vehicle, "Vehicles", "Ships, carts, sleds and siege vehicles"),
        (PrefabBrowserKind.Turret, "Turrets", "Turret components, including player-built and enemy ballistae"),
        (PrefabBrowserKind.Portal, "Portals", "TeleportWorld and Teleport components, including derived mod components"),
        (PrefabBrowserKind.Creature, "Creatures", "Character prefabs, including mobs and players"),
        (PrefabBrowserKind.AmbientCreature, "Ambient creatures", "RandomFlyingBird and Fish components, including derived mod components"),
        (PrefabBrowserKind.Ragdoll, "Ragdolls", "Ragdoll and corpse prefabs"),
        (PrefabBrowserKind.Spawner, "Spawners", "Creature and loot spawners, nests, hatching eggs and spawning abilities"),
        (PrefabBrowserKind.TriggerPoint, "Trigger points", "Trigger spawners, trigger abilities and grappling points"),
        (PrefabBrowserKind.Projectile, "Projectiles", "Projectile and area attack components"),
        (PrefabBrowserKind.Particle, "Particles", "Standalone particle effects, not particles embedded in persistent objects"),
        (PrefabBrowserKind.VisualFx, "Visual FX", "Effect references, standalone particles, camera shake, animated effects and temporary renderers"),
        (PrefabBrowserKind.Audio, "Audio", "Sound and music prefabs"),
        (PrefabBrowserKind.Destruction, "Destruction stages", "Prefabs referenced as damaged or destroyed replacements"),
        (PrefabBrowserKind.Fracture, "Fractures", "Multi-part MineRock5 models and referenced fragment prefabs"),
        (PrefabBrowserKind.Lod, "LOD models", "Prefabs with LODGroup, fade, light LOD or terrain LOD components"),
        (PrefabBrowserKind.Planting, "Planting stages", "Player crops, saplings and planted stumps"),
        (PrefabBrowserKind.Seed, "Seeds / planting inputs", "Items consumed by Plant pieces, regardless of their prefab names"),
        (PrefabBrowserKind.TerrainTool, "Terrain tools", "Cultivating, digging, raising ground and roads"),
        (PrefabBrowserKind.SystemController, "System controllers", "Runtime proxies, environment volumes, internal controllers and standalone outdoor generators"),
        (PrefabBrowserKind.Development, "Disabled / development", "Outputs of disabled recipes and disabled build-table pieces"),
        (PrefabBrowserKind.PlayerDeath, "Player death", "Player tombstones"),
        (PrefabBrowserKind.BossTrigger, "Boss triggers", "Boss summoning bowls and ritual sockets, not standalone event zones"),
        (PrefabBrowserKind.Cinematic, "Cinematics", "Cinematic actors and helpers"),
        (PrefabBrowserKind.Vegetation, "Trees / vegetation", "Tree and plant components, growth references and natural non-rock registry entries"),
        (PrefabBrowserKind.Geology, "Rocks / cliffs / ice", "MineRock components and otherwise unclassified mesh-collider nature prefabs"),
        (PrefabBrowserKind.Mineable, "Resource deposits", "MineRock components and non-building destructibles that drop materials"),
        (PrefabBrowserKind.Ruin, "Ruins / structural parts", "Non-player structural wear components and damaged world locations"),
        (PrefabBrowserKind.Container, "Containers / treasure", "Container and loot-cache components and destroyed-loot containers"),
        (PrefabBrowserKind.Useful, "Utility objects", "Traders, portals, navigation stones, altars, spawners, doors, crafting and processing stations, beds, fires and map tables"),
        (PrefabBrowserKind.Resource, "Trees / resource sources", "Harvestable trees, logs, beehives, sap collectors and material ItemDrop prefabs"),
        (PrefabBrowserKind.BuildingPiece, "Building pieces", "Piece or WearNTear models, including village roofs and walls; whole locations are separate"),
        (PrefabBrowserKind.Tree, "Harvestable trees / logs", "TreeBase, TreeLog and tree-type destructibles, excluding decorative vegetation"),
        (PrefabBrowserKind.Uncategorized, "Other / scenery", "Prefabs without a recognized category, including unknown modded names")
    ];

    private static Vector2 _scroll;

    internal static PrefabBrowserKind HiddenKinds { get; private set; } = PrefabBrowserKind.None;
    internal static PrefabBrowserKind OnlyKinds { get; private set; } = PrefabBrowserKind.None;

    internal static bool Includes(PrefabBrowserFilter filter, string name) => filter.Includes(name, HiddenKinds, OnlyKinds);

    internal static bool Draw()
    {
        Fill(new Rect(24, 108, 252, 498), new Color(0.04f, 0.055f, 0.05f, 0.92f));
        Label(new Rect(32, 116, 236, 26), "Filters", TextStyle);
        bool changed = false;
        if (Button(new Rect(32, 150, 112, 28), new GUIContent("All", "Reset every category to Include")))
        {
            HiddenKinds = PrefabBrowserKind.None;
            OnlyKinds = PrefabBrowserKind.None;
            changed = true;
        }
        if (Button(new Rect(152, 150, 116, 28), new GUIContent("None", "Set every category to Hide")))
        {
            HiddenKinds = PrefabBrowserKind.All;
            OnlyKinds = PrefabBrowserKind.None;
            changed = true;
        }
        _scroll = GUI.BeginScrollView(new Rect(28, 190, 244, 408), _scroll,
            new Rect(0, 0, 224, FilterOptions.Length * 54), false, false);
        for (int index = 0; index < FilterOptions.Length; index++)
        {
            var option = FilterOptions[index];
            float top = index * 54;
            GUI.Label(new Rect(4, top, 216, 22), new GUIContent(option.Text, option.Tooltip), RowStyle);
            int currentMode = (HiddenKinds & option.Kind) != 0 ? 1 : (OnlyKinds & option.Kind) != 0 ? 2 : 0;
            for (int mode = 0; mode < 3; mode++)
            {
                string label = mode == 0 ? "Include" : mode == 1 ? "Hide" : "Only";
                string tooltip = mode == 0 ? $"Do not restrict {option.Text}"
                    : mode == 1 ? $"Exclude {option.Text}" : $"Show only {option.Text}; reset every other filter";
                if (!Button(new Rect(4 + mode * 72, top + 24, 70, 24), new GUIContent(label, tooltip), currentMode == mode)
                    || currentMode == mode && mode != 2) continue;
                if (mode == 2)
                {
                    HiddenKinds = PrefabBrowserKind.None;
                    OnlyKinds = option.Kind;
                }
                else
                {
                    HiddenKinds &= ~option.Kind;
                    OnlyKinds &= ~option.Kind;
                    if (mode == 1) HiddenKinds |= option.Kind;
                }
                changed = true;
            }
        }
        GUI.EndScrollView();
        return changed;
    }
}
