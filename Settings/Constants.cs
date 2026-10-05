using JoksterCube.PingMark.Common;
using BepInEx.Configuration;
using UnityEngine;

namespace JoksterCube.PingMark.Settings;

internal static class Constants
{
    internal static class Plugin
    {
        internal const string ModName = "PingMark";
        internal const string ModVersion = "1.0.2";
        internal const string Author = "JoksterCube";
        internal const string ModGUID = $"{Author}.{ModName}";
        internal const string Description = "Mark loot, creatures, and locations with named pings, colored outlines, and live distances. Share discoveries with your party.";
        internal const string Copyright = "Copyright ©  2026";
        internal const string Guid = "7b25968d-61be-412f-b20f-36c18da9de86";

        internal const string ConfigFileName = $"{ModGUID}.cfg";
    }

    internal static class DebugMessages
    {
        internal const string ReadConfigCalled = "ReadConfigCalled called";
        internal const string ErrorLoadingConfig = $"There was an issue loading your {Plugin.ConfigFileName}";
        internal const string RequestCheckConfig = "Please check your config entries for spelling and format!";
    }

    internal static class LayerMasks
    {
        internal static readonly int MobHighlight = LayerMask.GetMask(
            "Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle",
            "character", "character_net", "character_ghost", "character_noenv", "item");
    }

    internal static class Groups
    {
        internal static class General
        {
            internal const string Group = "1 - General";

            internal static readonly ConfigInfo<Toggle> Enabled = new(
                Group,
                "Enabled",
                "Enable all PingMark features.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> Lock = new(
                Group,
                "Lock Configuration",
                "If on, synchronized configuration is locked and can be changed by server admins only.",
                Toggle.On,
                true);

            internal static readonly ConfigInfo<float> RefreshInterval = new(
                Group,
                "Refresh Interval",
                new ConfigDescription(
                    "How often, in seconds, distances on visible pings and highlighted mobs are recalculated.",
                    new AcceptableValueRange<float>(0.05f, 5f)),
                0.25f,
                true);

            internal static readonly ConfigInfo<float> PingCooldown = new(
                Group,
                "Ping Cooldown",
                new ConfigDescription(
                    "Minimum seconds between location pings sent with Z. Set to 0 to disable.",
                    new AcceptableValueRange<float>(0f, 60f)),
                0f,
                true);

            internal static readonly ConfigInfo<float> ZPingBroadcastDistance = new(
                Group,
                "Z Ping Broadcast Distance",
                new ConfigDescription(
                    "Maximum distance in meters from the sending player to PingMark recipients of Z location pings and character highlights. Map pings remain global. Set to 0 for unlimited. Without a modded server, the sender's setting is used.",
                    new AcceptableValueRange<float>(0f, 10000f)),
                200f,
                true);

            internal static readonly ConfigInfo<Toggle> SendZPingsToUnmoddedPlayers = new(
                Group,
                "Send Z Pings To Unmodded Players",
                "Send Z pings as ordinary location pings to players without PingMark, regardless of distance. Character pings use the character's current position. If off, players without PingMark receive no Z pings.",
                Toggle.On,
                true);

            internal static readonly ConfigInfo<Toggle> DebugPrefabId = new(
                Group,
                "Debug Ping Names",
                "Show all names found on the hit object (prefab ID, location ID, hover, piece, etc.) in a top-left message when camera pinging.",
                Toggle.Off,
                false);
        }

        internal static class PingLabel
        {
            internal const string Group = "2 - Ping Label";

            internal static readonly ConfigInfo<Toggle> ShowPingerName = new(
                Group,
                "Show Pinger Name",
                "Show the pinger's name on world/map ping labels and above highlighted mobs and players.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowDistance = new(
                Group,
                "Show Distance",
                "Show distance on world/map ping labels and above highlighted mobs and players.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<string> EmptyLabelText = new(
                Group,
                "Empty Label Text",
                "Text shown when the pinger name, target name, and distance are all hidden.",
                "PING",
                false);

            internal static readonly ConfigInfo<Toggle> UseKilometers = new(
                Group,
                "Use Kilometers",
                "Show distances of 1000 meters or more in kilometers with two decimal places.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Toggle> ShowTargetName = new(
                Group,
                "Show Target Name",
                "Show the pinged location/object name on this client. Target names are sent regardless of the sender's display setting.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<Color> PingColor = new(
                Group,
                "Ping Color",
                "Color of in-world ping labels (name and distance) when not matched to an outline color.",
                Color.white,
                false);

            internal static readonly ConfigInfo<Toggle> MatchTextToOutline = new(
                Group,
                "Match Ping Text To Outline",
                "Color the ping label with the outline color of the pinged target.",
                Toggle.Off,
                false);
        }

        internal static class Outlines
        {
            internal const string Group = "5 - Outlines";

            internal static readonly ConfigInfo<Toggle> PingOutlineEnabled = new(
                Group,
                "Enable Ping Outline",
                "Draw an outline around pinged locations, valuables/resources, drops and other models.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<float> OutlineWidth = new(
                Group, "Outline Width",
                new ConfigDescription("Width in pixels of outlines around highlighted mobs and pinged models. Visible through obstacles. Set to 0 to disable.", new AcceptableValueRange<float>(0f, 10f)),
                2f,
                false);

            internal static readonly ConfigInfo<Color> PingDropColor = new(
                Group,
                "Ping Drop Color",
                "Outline color for pinged dropped items (weapons, armor, materials on the ground).",
                new Color(0.7f, 0.5f, 1f),
                false);

            internal static readonly ConfigInfo<Color> PingPlayerColor = new(
                Group,
                "Ping Gravestone/Player Color",
                "Outline color for pinged gravestones and other players.",
                new Color(1f, 0.5f, 0.15f),
                false);

            internal static readonly ConfigInfo<Color> PingAnythingColor = new(
                Group,
                "Ping Anything Color",
                "Outline color for any other pinged model.",
                Color.white,
                false);

            internal static readonly ConfigInfo<Color> HostileOutlineColor = new(
                Group,
                "Hostile Outline Color",
                "Outline color for highlighted hostile mobs.",
                new Color(0.9f, 0.18f, 0.12f),
                false);

            internal static readonly ConfigInfo<Color> AllyOutlineColor = new(
                Group,
                "Ally Outline Color",
                "Outline color for highlighted ally and tamed mobs.",
                new Color(0.5f, 0.85f, 0.3f),
                false);
        }

        internal static class PrefabCollections
        {
            internal const string Group = "7 - Prefab Collections";

            internal const int Color1Id = 1;
            internal const int Color2Id = 2;
            internal const int Color3Id = 3;
            internal const int Color4Id = 4;
            internal const int Color5Id = 5;
            internal const int Color6Id = 6;
            internal const int Color7Id = 7;
            internal const int Color8Id = 8;
            internal const int Color9Id = 9;
            internal const int Color10Id = 10;
            internal const int Color11Id = 11;
            internal const int Color12Id = 12;

            internal static readonly ConfigInfo<Color> Color1 = new(Group, "Color 1", "Section palette color 1.", new Color(0.8f, 0.8f, 0.8f), false);
            internal static readonly ConfigInfo<Color> Color2 = new(Group, "Color 2", "Section palette color 2.", Color.white, false);
            internal static readonly ConfigInfo<Color> Color3 = new(Group, "Color 3", "Section palette color 3.", new Color(0.95f, 0.25f, 0.2f), false);
            internal static readonly ConfigInfo<Color> Color4 = new(Group, "Color 4", "Section palette color 4.", new Color(1f, 0.5f, 0.15f), false);
            internal static readonly ConfigInfo<Color> Color5 = new(Group, "Color 5", "Section palette color 5.", new Color(1f, 0.82f, 0.3f), false);
            internal static readonly ConfigInfo<Color> Color6 = new(Group, "Color 6", "Section palette color 6.", new Color(0.65f, 0.85f, 0.25f), false);
            internal static readonly ConfigInfo<Color> Color7 = new(Group, "Color 7", "Section palette color 7.", new Color(0.25f, 0.75f, 0.35f), false);
            internal static readonly ConfigInfo<Color> Color8 = new(Group, "Color 8", "Section palette color 8.", new Color(0.25f, 0.8f, 0.7f), false);
            internal static readonly ConfigInfo<Color> Color9 = new(Group, "Color 9", "Section palette color 9.", new Color(0.25f, 0.75f, 1f), false);
            internal static readonly ConfigInfo<Color> Color10 = new(Group, "Color 10", "Section palette color 10.", new Color(0.35f, 0.45f, 1f), false);
            internal static readonly ConfigInfo<Color> Color11 = new(Group, "Color 11", "Section palette color 11.", new Color(0.7f, 0.45f, 0.9f), false);
            internal static readonly ConfigInfo<Color> Color12 = new(Group, "Color 12", "Section palette color 12.", new Color(1f, 0.45f, 0.7f), false);
            internal static readonly ConfigInfo<Color>[] Colors =
            [Color1, Color2, Color3, Color4, Color5, Color6, Color7, Color8, Color9, Color10, Color11, Color12];

            internal static readonly ConfigInfo<int> UnassignedColorId = new(
                Group,
                "Unassigned Section Color ID",
                new ConfigDescription("Palette color ID for the Unassigned section.", new AcceptableValueRange<int>(Color1Id, Color12Id)),
                Color1Id,
                false);
        }

        internal static class MobHighlight
        {
            internal const string Group = "6 - Mob Highlight";

            internal static readonly ConfigInfo<Toggle> Enabled = new(
                Group,
                "Enable Mob Highlight",
                "Enable temporarily highlighting mobs with a visible nameplate.",
                Toggle.On,
                true);

            internal static readonly ConfigInfo<Toggle> OutlineEnabled = new(
                Group,
                "Enable Mob Outline",
                "Draw an outline around highlighted mobs.",
                Toggle.On,
                false);

            internal static readonly ConfigInfo<float> MobHighlightDuration = new(
                Group, "Mob Highlight Duration",
                new ConfigDescription("Seconds a highlighted mob's nameplate remains visible.", new AcceptableValueRange<float>(1f, 120f)),
                10f,
                true);

            internal static readonly ConfigInfo<int> MaxHighlightedMobs = new(
                Group, "Max Highlighted Mobs",
                new ConfigDescription("Maximum number of mob or player nameplates highlighted at once per sending player. Highlighting another replaces only that player's oldest highlight.", new AcceptableValueRange<int>(1, 20)),
                1,
                true);
        }

        internal static class Inputs
        {
            internal const string Group = "9 - Inputs";

            internal static readonly ConfigInfo<KeyboardShortcut> ToggleModShortcut = new(
                Group,
                "Toggle Mod Shortcut",
                "Toggle all PingMark features.",
                new KeyboardShortcut(KeyCode.P, KeyCode.RightControl),
                false);

            internal static readonly ConfigInfo<KeyboardShortcut> PrefabEditorShortcut = new(
                Group,
                "Prefab Editor Shortcut",
                "Press to open the prefab collection editor when its shortcut is enabled. With server-synced lists, only admins can edit.",
                new KeyboardShortcut(KeyCode.P, KeyCode.RightControl, KeyCode.RightShift),
                false);

            internal static readonly ConfigInfo<KeyboardShortcut> MobHighlightShortcut = new(
                Group,
                "Mob Highlight Shortcut",
                "Temporarily highlight a mob or enabled player under the camera crosshair, or ping the aimed location otherwise.",
                new KeyboardShortcut(KeyCode.Z),
                false);

            internal static readonly ConfigInfo<Toggle> PrefabEditorShortcutEnabled = new(
                Group, "Enable Prefab Editor Shortcut",
                "Enable the Prefab Editor Shortcut.",
                Toggle.On, false);
        }

        internal static class PlayerHighlight
        {
            internal const string Group = "8 - Player Highlight";

            internal static readonly ConfigInfo<float> Duration = new(
                Group,
                "Player Highlight Duration",
                new ConfigDescription("Seconds a pinged player remains highlighted.", new AcceptableValueRange<float>(1f, 120f)),
                5f,
                true);

            internal static readonly ConfigInfo<Toggle> HideOwnOutline = new(
                Group,
                "Hide Own Player Outline",
                "Hide the outline on your own character when another player pings you. Other recipients still see your outline when player pinging is enabled.",
                Toggle.On,
                true);
        }

        internal static class CameraPing
        {
            internal const string Group = "3 - Camera Ping";

            internal static readonly ConfigInfo<float> MaxDistance = new(
                Group,
                "Max Distance",
                new ConfigDescription(
                    "Maximum camera ping distance in meters. If nothing is hit, ping this far along the view direction.",
                    new AcceptableValueRange<float>(10f, 2000f)),
                500f,
                false);

            internal static readonly ConfigInfo<float> MaxUnderwaterDepth = new(
                Group,
                "Max Underwater Depth",
                new ConfigDescription(
                    "Maximum depth in meters below the water surface at which objects can be pinged. Deeper targets ping the water surface instead.",
                    new AcceptableValueRange<float>(0f, 100f)),
                10f,
                false);
        }

        internal static class PingTargets
        {
            internal const string Group = "4 - Ping Targets";

            internal static readonly ConfigInfo<string> PrefabSections = new(
                Group,
                "Prefab Sections",
                "Section definitions managed in the Prefab Collections editor.",
                                """
                                Version: 1
                                Sections:
                                - Name: Valuables
                                  Prefabs: AshCrow, AshlandsBush2, Ashlands_floor_large_fractured, Ashlands_Fortress_Wall_PillarTopStone_frac, Ashlands_Fortress_Wall_PillarTop_frac, Ashlands_Fortress_Wall_Pillar_base_frac, Ashlands_Fortress_Wall_Pillar_frac, Barnacle, BarnacleLava, barrell, barreltrap, Beehive_Destruction, BlackIceShard_01, BlackIceShard_02, blackmarble_altar_crystal, blackmarble_post01, BlueberryBush, Bush01, Bush01_deepnorth, Bush01_heath, CargoCrate, CastleKit_brazier, caverock_ice_stalagmite, Charred_altar_bellfragment, Chest, cliff_ashlands1_frac, cliff_ashlands2_frac, cliff_ashlands4_frac, cliff_ashlands6_frac, cliff_ashlands7_HalfArch_frac, cliff_ashlandsflowrock_frac, cliff_ashlands_Arch_frac, cliff_mistlands1_creep_frac, cliff_mistlands1_frac, cliff_mistlands2_frac, cloth_hanging_door, cloth_hanging_long, CloudberryBush, Crow, dungeon_iron_pile, dvergrprops_barrel, DvergrProps_extractor_crate, dvergrprops_pickaxe, dvergrtown_wood_beam, dvergrtown_wood_pole, dvergrtown_wood_support, dvergrtown_wood_wall01, dvergrtown_wood_wall02, dvergrtown_wood_wall03, elaking_trashpile, fenrirhide_hanging, FernFiddleHeadAshlands, FlametalRockstand, FlametalRockstand_frac, giant_brain, giant_brain_frac, giant_helmet1, giant_helmet1_destruction, giant_helmet2, giant_helmet2_destruction, giant_ribs, giant_ribs_frac, giant_skull, giant_skull_frac, giant_sword1, giant_sword1_destruction, giant_sword2, giant_sword2_destruction, goblin_totempole, goblin_trashpile, goldvein, goldvein_frac, GraveStone_Broken_CharredTwitcherNest, GraveStone_CharredTwitcherNest, GraveStone_Elite_Broken_CharredTwitcherNest, GraveStone_Elite_CharredTwitcherNest, Greydwarf_Root, Greydwarf_Surprise, GuckSack, GuckSack_small, hanging_hairstrands, HeathRockPillar_frac, highstone_2_frac, highstone_frac, HoleRock_root1, HoleRock_rootBush1, HoleRock_rootFloor1, HugeRoot1, ice1, IcePond_rock_frac, IceShard_01, IceShard_02, IceShard_03, IceShard_04, IceShard_05, IceShard_06, IceShore, IceShore_frac, Ice_floor_fractured, ice_rock1, ice_rock1_frac, incinerator, IronOre_scraps, LargeBone, LargeBone_half01, LargeBone_half02, Leviathan, LeviathanLava, LingonberryBush, loot_chest_stone, loot_chest_wood, loot_deepNorth_Granary, loot_deepNorth_TimberHall, LuredFaderEmber, LuredWisp, marker01, marker02, MemorialStone_Large, MemorialStone_Medium, MemorialStone_Small, MineRock_Copper, MineRock_Iron, MineRock_Meteorite, MineRock_Meteorite_frac, MineRock_Obsidian, MineRock_Obsidian_frac, MineRock_Stone, MineRock_Tin, MineRock_Tin_frac, morgenhole_pile, Morkhalla_Bedroll1, Morkhalla_Bedroll2, Morkhalla_ChestAncient, Morkhalla_Eye1, Morkhalla_Eye2, Morkhalla_Eye3, Morkhalla_Eye4, Morkhalla_Eye5_gemstone, Morkhalla_Eye6_gemstone, Morkhalla_Eye7_gemstone, Morkhalla_Rubble1, Morkhalla_Rubble2, Morkhalla_Rubble3, Morkhalla_Rubble4, Morkhalla_rubble_trashpile, Morkhalla_Rug_corner, Morkhalla_Rug_end1, Morkhalla_Rug_end2, Morkhalla_Rug_middle, Morkhalla_Rug_stair, Morkhalla_WeaponStand, MountainGraveStone01, MountainKit_brazier, MountainKit_brazier_blue, mountainkit_chair, mountainkit_table, mudpile, mudpile2, mudpile2_frac, mudpile_beacon, mudpile_frac, mudpile_fragile, mudpile_old, Pickable_Ashstone, Pickable_Barley, Pickable_Barley_Wild, Pickable_BlackCoreStand, Pickable_Bloodbag, Pickable_BogIronOre, Pickable_Branch, Pickable_Branch_Snow, Pickable_Carrot, Pickable_Charredskull, Pickable_Dandelion, Pickable_DolmenTreasure, Pickable_DragonEgg, Pickable_DvergerThing, Pickable_DvergrLantern, Pickable_DvergrMineTreasure, Pickable_DvergrStein, Pickable_Fiddlehead, Pickable_Fishingrod, Pickable_Flax, Pickable_Flax_Wild, Pickable_Flint, Pickable_ForestCryptRandom, Pickable_ForestCryptRemains01, Pickable_ForestCryptRemains02, Pickable_ForestCryptRemains03, Pickable_ForestCryptRemains04, Pickable_FrostCoreHanger, Pickable_GlowWorm, Pickable_Guck, Pickable_Hairstrands, Pickable_Hairstrands01, Pickable_Hairstrands02, Pickable_HardRockOffspring, Pickable_Item, Pickable_Kale, Pickable_MeatPile, Pickable_MeatRemains, Pickable_Meteorite, Pickable_MoltenCoreStand, Pickable_MorkHallaTreasure, Pickable_MountainCaveCrystal, Pickable_MountainCaveObsidian, Pickable_MountainCaveRandom, Pickable_MountainRemains01_buried, Pickable_Mushroom, Pickable_Mushroom_blue, Pickable_Mushroom_JotunPuffs, Pickable_Mushroom_Magecap, Pickable_Mushroom_yellow, Pickable_Oat, Pickable_Obsidian, Pickable_Onion, Pickable_Poteitr, Pickable_Pot_Shard, Pickable_RandomFood, Pickable_RoyalJelly, Pickable_SeedCarrot, Pickable_SeedKale, Pickable_SeedOnion, Pickable_SeedTurnip, Pickable_SmokePuff, Pickable_Snowball, Pickable_Stone, Pickable_StoneRock, Pickable_SulfurRock, Pickable_SunkenCryptRandom, Pickable_SurtlingCoreStand, Pickable_Swordpiece1, Pickable_Swordpiece2, Pickable_Swordpiece3, Pickable_Tar, Pickable_TarBig, Pickable_Thistle, Pickable_Tin, Pickable_Turnip, Pickable_VoltureEgg, piece_beehive, piece_birdnest, piece_chest, piece_chest_barrel, piece_chest_blackmetal, piece_chest_grausten, piece_chest_private, piece_chest_warderobe, piece_chest_wood, piece_gift1, piece_gift2, piece_gift3, piece_pot1, piece_pot1_cracked, piece_pot1_red, piece_pot2, piece_pot2_cracked, piece_pot2_red, piece_pot3, piece_pot3_cracked, piece_pot3_red, piece_sapcollector, Placeable_Stone, PropFeastDeepNorth, prop_ashwood_bed, prop_bed02, prop_bonfire, prop_cauldron_ext1_spice, prop_cauldron_ext3_butchertable, prop_cauldron_ext5_mortarandpestle, prop_cauldron_ext6_rollingpins, prop_chest_warderobe, prop_FeastAshlands, prop_FeastMeadows, prop_forge_ext2, prop_forge_ext5, prop_hearth, prop_itemstand, prop_itemstand_TrophyDraugrElite, prop_itemstand_TrophyGoblinBrute, prop_itemstand_TrophyGoblinShaman, prop_itemstand_TrophyGreydwarf, prop_itemstand_TrophyGreydwarfBrute, prop_itemstand_TrophySeekerBrute, prop_piece_bench_runed, prop_piece_brazierfloor01, prop_piece_cauldron, prop_piece_chair03, prop_piece_cookingstation, prop_piece_MeadCauldron, prop_piece_workbench_ext1, prop_piece_workbench_ext2, prop_piece_workbench_ext3, prop_piece_workbench_ext4, prop_preptable, prop_Tankard, prop_wood_stack, RaspberryBush, rock1_mountain_frac, rock2_heath_frac, rock2_mountain_frac, rock3_ice_frac, rock3_mountain_1_frac, rock3_mountain_frac, rock3_silver, rock3_silver_frac, rock4_ashlands_frac, rock4_bigrock_frac, rock4_coast_frac, rock4_copper, rock4_copper_frac, rock4_forest_frac, rock4_heath_frac, RockFingerBroken_frac, RockFinger_frac, RockThumb_frac, Rock_3_deepnorth_frac, Rock_3_frac, Rock_destructible_test, rock_mistlands1_frac, root07, root08, root11, root12, Seagal, ShimmeringSand_rock_frac, shipwreck_karve_chest, shipwreck_vikingship_chest, shrub_2, shrub_2_heath, silvervein, silvervein_frac, stoneblock_fracture, stonechest, stonewall_2, stonewall_3, stubbe_spawner, StumpHut_frac, SulfurArch, tarlump1_frac, trader_wagon_destructable, TreasureChest_ashlands, TreasureChest_ashlands_fortress, TreasureChest_ashland_stone, TreasureChest_blackforest, TreasureChest_charredfortress, TreasureChest_deepnorth_village, TreasureChest_dvergrtower, TreasureChest_dvergrtown, TreasureChest_dvergr_loose_stone, TreasureChest_fCrypt, TreasureChest_forestcrypt, TreasureChest_forestcrypt_hildir, TreasureChest_heath, TreasureChest_heath_hildir, TreasureChest_meadows, TreasureChest_meadows_01, TreasureChest_meadows_02, TreasureChest_meadows_buried, TreasureChest_meadows_combat, TreasureChest_memorial_buried, TreasureChest_mistlands, TreasureChest_morkhalla, TreasureChest_mountaincave, TreasureChest_mountaincave_hildir, TreasureChest_mountains, TreasureChest_plains, TreasureChest_plainsfortress_hildir, TreasureChest_plains_stone, TreasureChest_sunkencrypt, TreasureChest_swamp, TreasureChest_trollcave, TrollFrost_Dead, TrollFrost_Frac, TrollFrost_Frac_arm, TrollFrost_Frac_legs, UnstableLavaRock, veg_skull_Ashlands, VineAsh, VineGreen, widestone_2_frac, widestone_frac, YggdrasilRoot
                                  Enabled: true
                                  ColorId: 5
                                - Name: Locations
                                  Prefabs: AbandonedLogCabin02, AbandonedLogCabin03, AbandonedLogCabin04, altar, AncientUpgradeStation, AshlandRuins, BearCave, BigRockClearing, BogWitch_Camp, Castle, CharredFortress, CharredRuins1, CharredRuins2, CharredRuins3, CharredRuins4, CharredTowerRuins1, CharredTowerRuins1_dvergr, CharredTowerRuins2, CharredTowerRuins3, CombatRuin01, Crypt3, Crypt4, DevBedchamber, DevCombatRange, DevCombatRing, DevDressingRoom, DevFloor1, DevForge, DevGarden, DevGround1, DevGround2, DevHouse1, DevHouse2, DevHouse3, DevHouse4, DevHouse5, DevHouseStart, DevKitchen, DevMageRoom, DevSoundTest, DevWall1, DevWall2, DevWallAsh, DG_Cave, DG_DvergrBoss, DG_DvergrTown, DG_ForestCrypt, DG_HalfBurried_ForestCrypt, DG_Hildir_Cave, DG_Hildir_ForestCrypt, DG_Hildir_PlainsFortress, DG_Hole, DG_MorkHalla, DG_SunkenCrypt, dirtfloor, DN_Bossroom, DN_hut01, Dolmen01, Dolmen02, Dolmen03, FimbulLocation01, Floor, forestcrypt_Bend1, forestcrypt_Bend2, forestcrypt_Corridor1, forestcrypt_Corridor2, forestcrypt_Corridor3, forestcrypt_EndCap, forestcrypt_EndCap2, forestcrypt_EndCap3, forestcrypt_entrance_large, forestcrypt_room1, Fort1, FortressRuins, FrozenShip01_DN, FrozenShip02_DN, FrozenShip03_DN, GoblinCamp1, GoblinCamp2, GoblinCamp2_1, GoblinHut01, GoblinHut02, GoblinHut03, Grave1, Greydwarf_camp2, Greydwarf_camp3, HalfBurried_ForestCrypt, Hildir_camp, Hildir_cave, Hildir_crypt, Hildir_plainsfortress, HotSpring1, HotSpring2, HotSpring3, Hugintest, IcePond1, InfestedTree01, LumberCamp, Maypole, Mistlands_DvergrBossEntrance1, Mistlands_DvergrTownEntrance1, Mistlands_DvergrTownEntrance2, Mistlands_Excavation1, Mistlands_Excavation2, Mistlands_Excavation3, Mistlands_Giant1, Mistlands_Giant2, Mistlands_GuardTower1_new, Mistlands_GuardTower1_ruined_new, Mistlands_GuardTower1_ruined_new2, Mistlands_GuardTower2_new, Mistlands_GuardTower3_new, Mistlands_GuardTower3_ruined_new, Mistlands_Harbour1, Mistlands_Lighthouse1_new, Mistlands_RockSpire1, Mistlands_Swords1, Mistlands_Swords2, Mistlands_Swords3, Mistlands_Viaduct1, Mistlands_Viaduct2, MorgenHole1, MorgenHole2, MorgenHole3, MorkBorg, MountainCave01, MountainCave02, MountainGrave01, MountainWell1, NorthMemorialPlace, NorthVillage, Pillar1, Pillar2, PlaceofMystery1, PlaceofMystery2, PlaceofMystery3, Props, Ruin1, Ruin2, Ruin3, shields, ShipSetting01, ShipSetting02, ShipSetting03, ShipWreck01, ShipWreck01_DN, ShipWreck02, ShipWreck02_DN, ShipWreck03, ShipWreck04, Skull1, StoneCircle, StoneHenge1, StoneHenge2, StoneHenge3, StoneHenge4, StoneHenge5, StoneHenge6, StoneHouse1, StoneHouse1_heath, StoneHouse2, StoneHouse2_heath, StoneHouse3, StoneHouse4, StoneHouse5, StoneHouse5_heath, StoneTower1, StoneTower2, StoneTower3, StoneTower4, StoneTowerRuins03, StoneTowerRuins04, StoneTowerRuins05, StoneTowerRuins05_leet, StoneTowerRuins07, StoneTowerRuins07_sunk, StoneTowerRuins08, StoneTowerRuins08_sunk, StoneTowerRuins09, StoneTowerRuins09_sunk, StoneTowerRuins10, StoneTowerRuins10_sunk, stonewall, SunkenCrypt1, SunkenCrypt2, SunkenCrypt3, SunkenCrypt4, SwampHut1, SwampHut1_1, SwampHut2, SwampHut2_1, SwampHut3, SwampHut3_1, SwampHut4, SwampHut5, SwampRuin1, SwampRuin2, SwampWell1, TarPit1, TarPit1_1, TarPit2, TarPit2_1, TarPit3, TarPit3_1, TheDarkestHole, TheHole01, TrollCave, TrollCave02, Vendor_BlackForest, Walls, WoodFarm1, WoodHouse1, WoodHouse10, WoodHouse11, WoodHouse12, WoodHouse13, WoodHouse2, WoodHouse3, WoodHouse4, WoodHouse5, WoodHouse6, WoodHouse7, WoodHouse8, WoodHouse9, WoodVillage1, WoodVillage2, xmastree
                                  Enabled: true
                                  ColorId: 9
                                - Name: Useful
                                  Prefabs: artisan_ext1, ashwood_bed, ashwood_door, BatteringRam, bed, blackforge, blackforge_ext1, blackforge_ext2_vise, blackforge_ext3_metalcutter, blackforge_ext4_gemcutter, blackforge_ext5_apron, BlackIce_Core, blastfurnace, BlobMorkBig, BogWitch, BogWitch_Fire_Pit, Bonemass, BonePileSpawner, BonePileSpawner_swamp, bonfire, BossStone_Bonemass, BossStone_DragonQueen, BossStone_Eikthyr, BossStone_Fader, BossStone_Moder, BossStone_TheElder, BossStone_TheQueen, BossStone_Yagluth, bow_projectile_fire, Candle_resin, Cart, CastleKit_groundtorch_unlit, Catapult, cauldron_ext1_spice, cauldron_ext3_butchertable, cauldron_ext4_pots, cauldron_ext5_mortarandpestle, cauldron_ext6_rollingpins, cauldron_ext7_smoker, caverock_ice_pillar_wall, CelestialShard, charcoal_kiln, CharredStone_Spawner, cloth_hanging_door_double, Crypt2, darkwood_gate, DN_gammeltrollFrac01, DN_gammeltrollFrac02, Dragonqueen, DrakeLorestone, DrakeNest01, dungeon_forestcrypt_door, dungeon_queen_door, dungeon_sunkencrypt_irongate, dvergrtown_secretdoor, dvergrtown_slidingdoor, Eikthyrnir, eitrrefinery, EvilHeart_Forest, EvilHeart_Swamp, FaderLocation, fader_bellholder, fermenter, Fire, FireHole, fire_pit, fire_pit_haldor, fire_pit_hildir, fire_pit_iron, flametal_gate, forestcrypt_new_Burialchamber04, forge, forge_ext1, forge_ext2, forge_ext3, forge_ext4, forge_ext5, forge_ext6, FrozenGD, FrozenSkeleton_Pose1, FrozenSkeleton_Pose2, fuling_turret, GDKing, GoblinKing, GrapplingPoint, GrapplingPointSecondary, GraveStone_Broken_World, Greydwarf_camp1, Haldor, hearth, Hildir, HildirChest1, HildirChest2, HildirChest3, HoleRock_rootWall1, hole_destructableDoor, hole_destructableDoor1, hole_destructableDoor2, HouseFire, iron_grate, Karve, LavaRock, LootSpawner_pineforest, Mistlands_RoadPost1, Mistlands_Statue1, Mistlands_Statue2, Mistlands_StatueGroup1, Morkborg_gate, Morkhalla_Drawbridge, Morkhalla_firepit, Morkhalla_jotun_gate, MountainKit_wood_gate, offeraltar_FrozenKing_bossroom, piece_artisanstation, piece_bathtub, piece_bed02, piece_brazierceiling01, piece_brazierfloor01, piece_brazierfloor02, piece_cartographytable, piece_cauldron, piece_Charred_Balista, piece_cookingstation, piece_cookingstation_iron, piece_drawbridge, piece_drawbridge_log, piece_dvergr_wood_door, piece_EternalPyre, piece_FaderEmbers, piece_FrostFoundry, piece_FrostKiln, piece_groundtorch, piece_groundtorch_blue, piece_groundtorch_green, piece_groundtorch_wood, piece_hexagonal_door, piece_jackoturnip, piece_magetable, piece_magetable_ext, piece_magetable_ext2, piece_magetable_ext3, piece_magetable_ext4, piece_MeadCauldron, piece_oven, piece_preptable, piece_snowlantern, piece_spinningwheel, piece_stonecutter, piece_turret, piece_walltorch, piece_wisplure, piece_workbench, piece_workbench_ext1, piece_workbench_ext2, piece_workbench_ext3, piece_workbench_ext4, portal, portal_stone, portal_wood, projectile_meteor, Raft, Runestone_Ashlands, Runestone_BlackForest, Runestone_Boars, Runestone_DeepNorth, Runestone_Draugr, Runestone_Greydwarfs, Runestone_Meadows, Runestone_Mistlands, Runestone_Mountains, Runestone_Plains, Runestone_Swamps, Sled, smelter, Spawner_Bat, Spawner_Bjorn_sleeping, Spawner_Blob, Spawner_BlobElite, Spawner_BlobTar, Spawner_BlobTar_respawn_30, Spawner_Boar, Spawner_BogWitchKvastur_respawn_30, Spawner_Brood, Spawner_Charred, Spawner_CharredCross, Spawner_CharredStone, Spawner_CharredStone_Elite, Spawner_CharredStone_event, Spawner_Charred_Archer, Spawner_Charred_balista, Spawner_Charred_Dyrnwyn, Spawner_Charred_Mage, Spawner_Chicken, Spawner_Cultist, Spawner_Cultist_Hildir, Spawner_Cultist_Hildir_bossroom, Spawner_Draugr, Spawner_DraugrPile, Spawner_Draugr_Elite, Spawner_Draugr_Noise, Spawner_Draugr_Ranged, Spawner_Draugr_Ranged_Noise, Spawner_Draugr_respawn_30, Spawner_DvergerArbalest, Spawner_DvergerAshlands, Spawner_DvergerDeepNorth, Spawner_DvergerMage, Spawner_DvergerRandom, Spawner_ElakingMole_Wakeup, Spawner_FallenValkyrie, Spawner_Fenring, Spawner_Fish4, Spawner_Frysling, Spawner_Frysling_respawn_30, Spawner_Ghost, Spawner_Ghost_sleeping, Spawner_Ghost_Void, Spawner_Goblin, Spawner_GoblinArcher, Spawner_GoblinBrute, Spawner_GoblinBrute_Hildir, Spawner_GoblinDeepNorth, Spawner_GoblinShaman, Spawner_Greydwarf, Spawner_GreydwarfNest, Spawner_Greydwarf_Elite, Spawner_Greydwarf_Shaman, Spawner_Greydwarf_Surprise, Spawner_Hatchling, Spawner_Hen, Spawner_Hole, Spawner_Hole_double, Spawner_imp, Spawner_imp_respawn, Spawner_JotunDualWield, Spawner_JotunWarrior, Spawner_JotunWitch, Spawner_Kvastur, Spawner_Leech_cave, Spawner_Location_Elite, Spawner_Location_Greydwarf, Spawner_Location_Shaman, Spawner_Morgen, Spawner_Morgen_wakeup, Spawner_Seeker, Spawner_SeekerBrute, Spawner_SeekerBrute_respawn_240, Spawner_Seeker_respawn_240, Spawner_ShadowPerson, Spawner_Skeleton, Spawner_Skeleton_hildir, Spawner_Skeleton_hildir_bossroom, Spawner_Skeleton_Meadows, Spawner_Skeleton_Meadows_night_noarcher, Spawner_Skeleton_Mountains, Spawner_Skeleton_Mountains_night_noarcher, Spawner_Skeleton_night_noarcher, Spawner_Skeleton_poison, Spawner_Skeleton_respawn_30, Spawner_Skeleton_rise, Spawner_Skeleton_Swamp, Spawner_Skeleton_Swamp_night_noarcher, Spawner_StoneGolem, Spawner_Tick, Spawner_Tick_stared, Spawner_Tick_stared_respawn_240, Spawner_Troll, Spawner_TrollFrost, Spawner_Twitcher, Spawner_Ulv, Spawner_Volture, Spawner_Wraith, Spawner_Writhan, staff_fireball_projectile, StartPlatform, StartTemple, StatueEvil, stave_gate, sunken_crypt_gate, Trailership, TriggerSpawner_Brood, TriggerSpawner_Seeker, Troll_Summoned, UpgradeStation, Vegvisir_Bonemass, Vegvisir_DNBoss, Vegvisir_Eikthyr, Vegvisir_Fader, Vegvisir_GDKing, Vegvisir_Hildir_Cave, Vegvisir_Hildir_Crypt, Vegvisir_Hildir_Tower, Vegvisir_Moder, Vegvisir_Queen, Vegvisir_Yagluth, VikingShip, VikingShip_Ashlands, VoltureNest, Waymarker01, Waymarker02, windmill, wood_door, wood_fence_gate, wood_gate, wood_window
                                  Enabled: true
                                  ColorId: 8
                                """,
                true);

            internal static readonly ConfigInfo<Toggle> DropsEnabled = new(
                Group,
                "Enable Drops",
                "Outline and name pinged dropped items (weapons, armor, materials on the ground).",
                Toggle.On,
                true);

            internal static readonly ConfigInfo<Toggle> GravestonesEnabled = new(
                Group,
                "Enable Gravestones",
                "Outline and name pinged gravestones.",
                Toggle.On,
                true);

            internal static readonly ConfigInfo<Toggle> PlayersEnabled = new(
                Group,
                "Enable Other Players",
                "Temporarily highlight pinged players instead of sending a ground ping. Player nameplates show distance when Show Distance is on.",
                Toggle.Off,
                true);

            internal static readonly ConfigInfo<float> PlayerHighlightDuration = PlayerHighlight.Duration;

            internal static readonly ConfigInfo<Toggle> HideOwnPlayerOutline = PlayerHighlight.HideOwnOutline;

            internal static readonly ConfigInfo<Toggle> OthersEnabled = new(
                Group,
                "Enable All Other",
                "Outline and name any other pinged model that is not in the lists or a dropped item.",
                Toggle.Off,
                true);
        }
    }
}
