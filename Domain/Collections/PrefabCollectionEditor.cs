using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Common.Networking;
using JoksterCube.PingMark.Domain.Collections;
using JoksterCube.PingMark.Domain.Enums;
using JoksterCube.PingMark.Domain.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static JoksterCube.PingMark.Domain.Collections.PrefabCollectionEditorUI;
using static JoksterCube.PingMark.Settings.PluginConfig;
using SectionColorPalette = JoksterCube.PingMark.Settings.Constants.Groups.PrefabCollections;

namespace JoksterCube.PingMark.Domain.Collections;

internal static class PrefabCollectionEditor
{
    private static readonly HashSet<string> Selected = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<Vector2> Scroll = [];
    private static readonly List<string[]> Visible = [];
    private static readonly List<float> ColumnWidths = [];
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
    private static PrefabCollections? _collections;
    private static List<PrefabCollectionSection> _sections = [];
    private static IEnumerator<int>? _loading;
    private static string[]? _cachedNames;
    private static int _openedFrame;
    private static PrefabBrowserFilter _filter = new();
    private static PrefabBrowserKind _hiddenKinds = PrefabBrowserKind.None;
    private static PrefabBrowserKind _onlyKinds = PrefabBrowserKind.None;
    private static Vector2 _filterScroll;
    private static string _original = string.Empty;
    private static string _initialStaged = string.Empty;
    private static string _staged = string.Empty;
    private static string _search = string.Empty;
    private static string _status = string.Empty;
    private static int _openColorPicker = -1;
    private static Vector2 _collectionScroll;
    private static int _unassignedColorId;
    private static int _initialUnassignedColorId;
    private static bool _showClosePrompt;
    private static bool _showAutoSortPrompt;
    private static readonly Stack<(PrefabCollections Collections, List<PrefabCollectionSection> Sections)> AutoSortUndo = new();
    private static int _closedFrame = -1;
    private static GUIStyle? _text;
    private static GUIStyle? _title;
    private static GUIStyle? _count;
    private static GUIStyle? _row;
    private static GUIStyle? _button;
    private static GUIStyle? _searchStyle;
    private static Sprite? _panel;
    private static Sprite? _buttonSprite;

    internal static bool IsOpen => _collections != null;
    internal static bool BlocksInput => IsOpen || _closedFrame == Time.frameCount;
    internal static bool CanEdit => !RpcHandlers.ShouldWaitForInitialConfigSync && Plugin.CanEditPrefabCollections;

    internal static void Open()
    {
        if (IsOpen || !CanEdit || !ZNetScene.instance) return;
        _original = CurrentConfig();
        _sections = PrefabCollectionSectionConfig.LoadCopies();
        if (_cachedNames == null) _filter = new PrefabBrowserFilter();
        _collections = new PrefabCollections(_cachedNames ?? [], _sections);
        _initialStaged = StagedConfig();
        _search = string.Empty;
        _status = _cachedNames == null ? "Loading prefabs..." : string.Empty;
        _text = null;
        _row = null;
        Selected.Clear();
        _openColorPicker = -1;
        _showClosePrompt = false;
        _showAutoSortPrompt = false;
        AutoSortUndo.Clear();
        LoadHeaderColors();
        _collectionScroll = Vector2.zero;
        ResetColumnState();
        Refresh();
        _openedFrame = Time.frameCount;
        _loading = _cachedNames == null ? LoadCollections().GetEnumerator() : null;
        ReleaseMouse();
    }

    internal static void ReleaseMouse()
    {
        ZCursor.LockState = CursorLockMode.None;
        ZCursor.Show();
    }

    internal static void Close()
    {
        if (!IsOpen) return;
        _loading?.Dispose();
        _loading = null;
        _collections = null;
        _sections = [];
        Selected.Clear();
        _showClosePrompt = false;
        _showAutoSortPrompt = false;
        AutoSortUndo.Clear();
        _closedFrame = Time.frameCount;
    }

    internal static void Update()
    {
        if (!IsOpen) return;
        Player? player = Player.m_localPlayer;
        if (!CanEdit || !ZNetScene.instance || !player || player!.IsDead() || player.IsTeleporting()
            || player.IsSleeping() || player.InCutscene() || !Hud.instance)
        {
            Close();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_showAutoSortPrompt) _showAutoSortPrompt = false;
            else Close();
            return;
        }
        if (_loading == null || Time.frameCount == _openedFrame) return;
        try
        {
            Stopwatch budget = Stopwatch.StartNew();
            for (int step = 0; step < LoadingStepsPerFrame && budget.Elapsed.TotalMilliseconds < LoadingBudgetMilliseconds; step++)
            {
                if (!_loading.MoveNext())
                {
                    _loading.Dispose();
                    _loading = null;
                    _status = string.Empty;
                    break;
                }
                _status = $"Loading prefabs... {_loading.Current:N0} found";
            }
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogError($"Unable to load prefab collections: {exception}");
            Close();
        }
    }

    private static IEnumerable<int> LoadCollections()
    {
        PrefabFeatureClassifier classifier = new(_filter);
        foreach (int progress in classifier.BuildSteps()) yield return progress;
        string[] names = classifier.Names();
        _collections = new PrefabCollections(names, _sections);
        yield return names.Length;
        Refresh(false);
        for (int index = 0; index < _collections!.CollectionCount; index++)
        {
            ColumnWidths[index] = DefaultColumnWidth;
            foreach (string name in Visible[index])
            {
                if (_row != null)
                    ColumnWidths[index] = Mathf.Max(ColumnWidths[index], _row.CalcSize(new GUIContent(name)).x + ColumnTextPadding);
                yield return names.Length;
            }
        }
        _cachedNames = names;
    }

    internal static void Draw(Plugin plugin)
    {
        if (!IsOpen) return;
        EnsureStyles();
        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        bool previousEnabled = GUI.enabled;
        int previousDepth = GUI.depth;
        GUI.depth = -100;
        DrawScreenBackdrop(new Color(0, 0, 0, 0.72f));
        float scale = Mathf.Min(MaximumCanvasScale,
            Mathf.Min(Screen.width / (float)ScaleReferenceWidth, Screen.height / (float)ScaleReferenceHeight));
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - CanvasWidth * scale) / 2,
            (Screen.height - CanvasHeight * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
        try
        {
            DrawPanel(new Rect(0, 0, CanvasWidth, CanvasHeight), _panel);
            Label(new Rect(24, 16, 800, 38), "PingMark | Prefab Collections", _title);
            GUI.enabled = previousEnabled && _loading == null && !_showClosePrompt && !_showAutoSortPrompt;
            if (Button(new Rect(852, 18, 136, 34), new GUIContent("Auto Sort",
                "Replace all sections with auto-sorted Valuables, Locations and Useful sections. Confirmation required.")))
            {
                _openColorPicker = -1;
                GUI.FocusControl(null);
                _showAutoSortPrompt = true;
            }
            if (Button(new Rect(1000, 18, 128, 34), new GUIContent("+ Section", "Add a prefab section")))
                AddSection();
            GUI.enabled = previousEnabled && !_showAutoSortPrompt;
            if (Button(new Rect(1142, 18, 34, 34), new GUIContent("X", "Close")))
            {
                if (HasPendingChanges()) _showClosePrompt = true;
                else
                {
                    Close();
                    Event.current.Use();
                    return;
                }
            }
            GUI.enabled = previousEnabled && _loading == null && !_showClosePrompt && !_showAutoSortPrompt;
            Label(new Rect(24, 64, 100, 32), "Search", _text);
            GUI.SetNextControlName("PingMarkPrefabSearch");
            string search = GUI.TextField(new Rect(134, 64, 800, 32), _search, _searchStyle);
            if (search != _search)
            {
                _search = search;
                Selected.Clear();
                ResetScrollPositions();
                Refresh();
            }
            if (Button(new Rect(946, 64, 230, 32), new GUIContent("Select Visible")))
                foreach (string[] column in Visible)
                    foreach (string name in column) Selected.Add(name);

            if (DrawFilters())
            {
                Selected.Clear();
                ResetScrollPositions();
                Refresh();
            }

            DrawColumn(0, 292, true);
            float sectionContentWidth = Mathf.Max(MinimumSectionContentWidth,
                _sections.Count * SectionColumnStride - SectionContentPadding);
            _collectionScroll = GUI.BeginScrollView(new Rect(514, 108, 662, 498), _collectionScroll,
                new Rect(0, 0, sectionContentWidth, 498), false, false);
            for (int index = 1; index < _collections!.CollectionCount; index++)
                DrawColumn(index, (index - 1) * SectionColumnStride, false);
            GUI.EndScrollView();

            Label(new Rect(24, 618, 174, 32), $"Selected: {Selected.Count}", _text);
            GUI.enabled = previousEnabled && _loading == null && Selected.Count > 0 && !_showClosePrompt && !_showAutoSortPrompt;
            if (Button(new Rect(210, 618, 198, 32), new GUIContent("Unselect all", "Clear selection without moving any prefabs")))
                Selected.Clear();
            GUI.enabled = previousEnabled && !_showClosePrompt && !_showAutoSortPrompt;
            Label(new Rect(24, 656, 1152, 26), _status.Length > 0 ? _status : GUI.tooltip, _text);
            GUI.enabled = previousEnabled && _loading == null && (_collections?.CanUndo == true || AutoSortUndo.Count > 0)
                && !_showClosePrompt && !_showAutoSortPrompt;
            if (Button(new Rect(708, 690, 156, 34), new GUIContent("Undo", "Undo last move")))
            {
                if (_collections?.Undo() != true && AutoSortUndo.Count > 0)
                {
                    (_collections, _sections) = AutoSortUndo.Pop();
                    _openColorPicker = -1;
                    ResetColumnState();
                    ResetScrollPositions();
                }
                Selected.Clear();
                Refresh();
            }
            GUI.enabled = previousEnabled && !_showClosePrompt && !_showAutoSortPrompt;
            if (Button(new Rect(876, 690, 144, 34), new GUIContent("Cancel"))) Close();
            GUI.enabled = previousEnabled && CanEdit && _loading == null && _collections != null && HasPendingChanges()
                && !_showClosePrompt && !_showAutoSortPrompt;
            if (Button(new Rect(1032, 690, 144, 34), new GUIContent("Apply"))) Apply(plugin);
            if (_showClosePrompt) DrawClosePrompt(plugin, previousEnabled);
            if (_showAutoSortPrompt) DrawAutoSortPrompt(previousEnabled);
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.enabled = previousEnabled;
            GUI.depth = previousDepth;
        }
        if (Event.current.isMouse || Event.current.isKey || Event.current.type == EventType.ScrollWheel)
            Event.current.Use();
    }

    private static void DrawColumn(int index, float left, bool unassigned)
    {
        if (_collections == null) return;
        float yOffset = unassigned ? 0 : -108;
        if (_openColorPicker == index) HandleColorPickerInput(index, left, yOffset);
        Color color = GetColumnColor(index);
        Fill(new Rect(left, 108 + yOffset, 214, 498), new Color(0.04f, 0.055f, 0.05f, 0.92f));
        Fill(new Rect(left, 108 + yOffset, 214, 3), color);
        if (unassigned)
            Label(new Rect(left + 8, 116 + yOffset, 162, 26), "Unassigned", _text);
        else
        {
            PrefabCollectionSection section = _sections[index - 1];
            string name = GUI.TextField(new Rect(left + 8, 116 + yOffset, 132, 26), section.Name, _searchStyle);
            if (name != section.Name)
            {
                section.Name = name;
                _staged = StagedConfig();
            }
            bool enabled = GUI.Toggle(new Rect(left + 8, 142 + yOffset, 20, 18), section.Enabled,
                new GUIContent(string.Empty, $"Enable {section.Name}"));
            if (enabled != section.Enabled)
            {
                section.Enabled = enabled;
                _staged = StagedConfig();
            }
        }
        Rect colorButton = new(left + (unassigned ? 178 : 150), 116 + yOffset, 28, 24);
        Fill(colorButton, color);
        Color border = colorButton.Contains(Event.current.mousePosition) ? new Color(1f, 0.9f, 0.65f) : new Color(0.55f, 0.55f, 0.5f);
        DrawBorder(colorButton, border);
        string sectionName = unassigned ? "Unassigned" : _sections[index - 1].Name;
        if (GUI.Button(colorButton, new GUIContent(string.Empty, $"Change {sectionName} section color"), GUIStyle.none))
            _openColorPicker = _openColorPicker == index ? -1 : index;
        bool wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && (unassigned || _sections.Count > 1);
        bool removeSection = !unassigned
            && Button(new Rect(left + 182, 116 + yOffset, 24, 24), new GUIContent("-", $"Remove {sectionName} section"));
        GUI.enabled = wasEnabled;
        if (removeSection)
        {
            _sections.RemoveAt(index - 1);
            _collections.RemoveSection(index);
            Selected.Clear();
            _openColorPicker = -1;
            Refresh();
            return;
        }
        string[] names = Visible[index];
        Label(new Rect(left + (unassigned ? 8 : 32), 142 + yOffset, unassigned ? 198 : 174, 18),
            $"{names.Length:N0} / {_collections.Count(index):N0} prefabs", _count);
        Rect viewport = new(left + 4, 164 + yOffset, 206, 326);
        float contentWidth = ColumnWidths[index];
        Scroll[index] = GUI.BeginScrollView(viewport, Scroll[index], new Rect(0, 0, contentWidth, names.Length * PrefabRowHeight), false, false);
        int first = Mathf.Max(0, Mathf.FloorToInt(Scroll[index].y / PrefabRowHeight));
        int last = Mathf.Min(names.Length, first + VisibleRowsPerColumn);
        for (int rowIndex = first; rowIndex < last; rowIndex++)
        {
            string name = names[rowIndex];
            Rect rect = new(0, rowIndex * PrefabRowHeight, contentWidth, PrefabRowHeight);
            if (Selected.Contains(name)) Fill(rect, new Color(color.r, color.g, color.b, 0.28f));
            if (GUI.Button(rect, new GUIContent(name, $"{name} | {_filter.Kind(name)}"), _row))
            {
                if (!Selected.Add(name)) Selected.Remove(name);
            }
        }
        GUI.EndScrollView();
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && Selected.Count > names.Count(Selected.Contains);
        if (Button(new Rect(left + 8, 498 + yOffset, 198, 28),
            new GUIContent("Move", $"Move selection to {sectionName}")))
        {
            _collections.Move(Selected, index);
            Selected.Clear();
            Refresh();
            names = Visible[index];
        }
        GUI.enabled = previousEnabled;
        if (Button(new Rect(left + 8, 534 + yOffset, 198, 28),
                new GUIContent("Copy CSV", $"Copy {sectionName} prefabs matching the current search and filters")))
        {
            GUIUtility.systemCopyBuffer = string.Join(", ", names);
            _status = $"Copied {names.Length:N0} {sectionName} prefabs.";
        }
        if (Button(new Rect(left + 8, 570 + yOffset, 198, 28),
                new GUIContent("Select all", $"Select all {sectionName} prefabs matching the current search and filters")))
            foreach (string name in names) Selected.Add(name);
        if (_openColorPicker == index) DrawColorPicker(index, left, yOffset);
    }

    private static void AutoSort()
    {
        if (_collections == null || !CanEdit || _loading != null) return;
        string[] names = Enumerable.Range(0, _collections.CollectionCount)
            .SelectMany(index => _collections.Names(index, string.Empty)).ToArray();
        List<PrefabCollectionSection> sections = PrefabAutoSort.CreateSections(names, _filter);
        PrefabCollections collections = new(names, sections);
        AutoSortUndo.Push((_collections, _sections));
        _sections = sections;
        _collections = collections;
        _showAutoSortPrompt = false;
        Selected.Clear();
        _openColorPicker = -1;
        ResetColumnState();
        ResetScrollPositions();
        Refresh();
        int sorted = names.Length - _collections.Count(0);
        _status = $"Auto sorted {sorted:N0} prefabs into {PrefabAutoSort.SectionNames.Length} replacement sections. Unknown/excluded IDs are Unassigned. Apply to save.";
    }

    private static void DrawAutoSortPrompt(bool previousEnabled)
    {
        DrawModalBackdrop();
        Rect panel = new(300, 245, 600, 250);
        DrawPanel(panel, _panel);
        Label(new Rect(panel.x + 24, panel.y + 16, panel.width - 48, 34), "Replace all sections?", _title);
        Label(new Rect(panel.x + 24, panel.y + 60, panel.width - 48, 30),
            "All sections will be replaced with Valuables, Locations and Useful.", _text);
        Label(new Rect(panel.x + 24, panel.y + 94, panel.width - 48, 30),
            "Custom names, colors and enabled states will be reset.", _text);
        Label(new Rect(panel.x + 24, panel.y + 128, panel.width - 48, 30),
            "Unknown/excluded IDs return to Unassigned. Apply saves changes.", _text);
        GUI.enabled = previousEnabled && CanEdit && _loading == null && _collections != null;
        if (Button(new Rect(panel.x + 44, panel.y + 192, 244, 36), new GUIContent("Replace and Auto Sort")))
            AutoSort();
        GUI.enabled = previousEnabled;
        if (Button(new Rect(panel.x + 312, panel.y + 192, 244, 36), new GUIContent("Cancel")))
            _showAutoSortPrompt = false;
    }

    private static void AddSection()
    {
        int number = _sections.Count + 1;
        string name = $"Section {number}";
        while (_sections.Any(section => string.Equals(section.Name, name, StringComparison.OrdinalIgnoreCase)))
            name = $"Section {++number}";
        _sections.Add(new PrefabCollectionSection
        {
            Name = name,
            ColorId = (_sections.Count + 4) % SectionColorPalette.Colors.Length + SectionColorPalette.Color1Id
        });
        _collections!.AddSection();
        Refresh();
        _collectionScroll.x = Mathf.Max(0, _sections.Count * SectionColumnStride - SectionScrollPadding);
    }

    private static void DrawClosePrompt(Plugin plugin, bool previousEnabled)
    {
        DrawModalBackdrop();
        Rect panel = new(370, 275, 460, 190);
        Fill(panel, PanelBackgroundColor);
        DrawBorder(panel, PanelBorderColor);
        Label(new Rect(panel.x + 20, panel.y + 16, panel.width - 40, 34), "Apply pending changes?", _title);
        Label(new Rect(panel.x + 20, panel.y + 58, panel.width - 40, 30),
            "Your prefab collections or section colors have changed.", _text);
        GUI.enabled = previousEnabled && CanEdit && _loading == null && _collections != null;
        if (Button(new Rect(panel.x + 44, panel.y + 116, 170, 36), new GUIContent("Apply"))) Apply(plugin);
        GUI.enabled = previousEnabled;
        if (Button(new Rect(panel.x + 246, panel.y + 116, 170, 36), new GUIContent("Cancel")))
            _showClosePrompt = false;
    }

    private static void DrawColorPicker(int index, float left, float yOffset)
    {
        Rect panel = PrefabCollectionEditorUI.ColorPickerPanel(left, yOffset);
        Fill(panel, new Color(0.02f, 0.025f, 0.02f, 0.98f));
        for (int option = 0; option < SectionColorPalette.Colors.Length; option++)
        {
            Rect swatch = PrefabCollectionEditorUI.ColorSwatch(panel, option);
            int colorId = option + SectionColorPalette.Color1Id;
            Color optionColor = GetPrefabSectionColor(colorId);
            Fill(swatch, optionColor);
            Color border = GetColumnColorId(index) == colorId ? new Color(1f, 0.9f, 0.65f) : new Color(0.45f, 0.45f, 0.4f);
            DrawBorder(swatch, border);
        }
    }

    private static void HandleColorPickerInput(int index, float left, float yOffset)
    {
        Event current = Event.current;
        Rect panel = PrefabCollectionEditorUI.ColorPickerPanel(left, yOffset);
        if (!panel.Contains(current.mousePosition)) return;
        if (current.type == EventType.MouseUp && current.button == 0)
            for (int option = 0; option < SectionColorPalette.Colors.Length; option++)
                if (PrefabCollectionEditorUI.ColorSwatch(panel, option).Contains(current.mousePosition))
                {
                    SetColumnColor(index, option + SectionColorPalette.Color1Id);
                    _staged = StagedConfig();
                    _openColorPicker = -1;
                    break;
                }
        if (current.isMouse) current.Use();
    }

    private static void LoadHeaderColors()
    {
        _unassignedColorId = Mathf.Clamp(UnassignedSectionColorId.Value, SectionColorPalette.Color1Id, SectionColorPalette.Colors.Length);
        _initialUnassignedColorId = _unassignedColorId;
    }

    private static int GetColumnColorId(int index) => index == 0 ? _unassignedColorId : _sections[index - 1].ColorId;

    private static Color GetColumnColor(int index) => GetPrefabSectionColor(GetColumnColorId(index));

    private static void SetColumnColor(int index, int colorId)
    {
        if (index == 0) _unassignedColorId = colorId;
        else _sections[index - 1].ColorId = colorId;
    }

    private static void ResetColumnState()
    {
        Scroll.Clear();
        Visible.Clear();
        ColumnWidths.Clear();
        EnsureColumnState();
    }

    private static void EnsureColumnState()
    {
        if (_collections == null) return;
        while (Scroll.Count < _collections.CollectionCount) Scroll.Add(Vector2.zero);
        while (Visible.Count < _collections.CollectionCount) Visible.Add([]);
        while (ColumnWidths.Count < _collections.CollectionCount) ColumnWidths.Add(DefaultColumnWidth);
        while (Scroll.Count > _collections.CollectionCount) Scroll.RemoveAt(Scroll.Count - 1);
        while (Visible.Count > _collections.CollectionCount) Visible.RemoveAt(Visible.Count - 1);
        while (ColumnWidths.Count > _collections.CollectionCount) ColumnWidths.RemoveAt(ColumnWidths.Count - 1);
    }

    private static void ResetScrollPositions()
    {
        for (int index = 0; index < Scroll.Count; index++) Scroll[index] = Vector2.zero;
        _collectionScroll = Vector2.zero;
    }

    private static bool HasPendingChanges() => _initialStaged != _staged || _unassignedColorId != _initialUnassignedColorId;

    private static void SaveHeaderColors()
    {
        UnassignedSectionColorId.Value = _unassignedColorId;
    }

    private static void Apply(Plugin plugin)
    {
        if (!CanEdit || _collections == null) { Close(); return; }
        if (_original != CurrentConfig())
        {
            _status = "Configuration changed. Close and reopen before applying.";
            return;
        }
        if (!ValidateSectionNames()) return;
        bool saveOnSet = plugin.Config.SaveOnConfigSet;
        try
        {
            plugin.Config.SaveOnConfigSet = false;
            PrefabCollectionSectionConfig.Save(_sections);
            SaveHeaderColors();
            if (Plugin.IsPrefabConfigSource) plugin.Config.Save();
        }
        catch (Exception exception)
        {
            Plugin.ModLogger.LogError($"Unable to save prefab collections: {exception}");
            _status = "Unable to save collections. Check the BepInEx log.";
            return;
        }
        finally { plugin.Config.SaveOnConfigSet = saveOnSet; }
        Close();
    }

    private static string CurrentConfig() => PrefabSections.Value;

    private static string StagedConfig()
    {
        if (_collections == null) return string.Empty;
        for (int index = 1; index < _collections.CollectionCount; index++)
            _sections[index - 1].Prefabs = _collections.Serialize(index);
        return PrefabCollectionSectionConfig.Serialize(_sections);
    }

    private static bool ValidateSectionNames()
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (PrefabCollectionSection section in _sections)
        {
            section.Name = section.Name.Trim();
            if (section.Name.Length == 0 || !names.Add(section.Name))
            {
                _status = "Section names must be non-empty and unique.";
                return false;
            }
        }
        return true;
    }

    private static void Refresh(bool updateWidths = true)
    {
        if (_collections == null) return;
        EnsureColumnState();
        for (int index = 0; index < _collections.CollectionCount; index++)
            Visible[index] = _collections.Names(index, _search).Where(name => _filter.Includes(name, _hiddenKinds, _onlyKinds)).ToArray();
        _staged = StagedConfig();
        if (updateWidths) UpdateColumnWidths();
    }

    private static void UpdateColumnWidths()
    {
        if (_row == null) return;
        for (int index = 0; index < _collections!.CollectionCount; index++)
        {
            float width = DefaultColumnWidth;
            foreach (string name in Visible[index])
                width = Mathf.Max(width, _row.CalcSize(new GUIContent(name)).x + ColumnTextPadding);
            ColumnWidths[index] = width;
        }
    }

    private static void EnsureStyles()
    {
        if (_text != null) return;
        TMP_Text? menuText = Menu.instance && Menu.instance.m_continueButton
            ? Menu.instance.m_continueButton.GetComponentInChildren<TMP_Text>(true) : null;
        Font? font = menuText && menuText!.font ? menuText.font.sourceFontFile : null;
        _text = new GUIStyle(GUI.skin.label) { font = font ? font : GUI.skin.font, fontSize = 16, alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Clip };
        _text.normal.textColor = new Color(0.92f, 0.87f, 0.72f);
        _title = new GUIStyle(_text) { fontSize = 22 };
        _count = new GUIStyle(_text) { fontSize = 12 };
        _count.normal.textColor = new Color(0.72f, 0.72f, 0.65f);
        _row = new GUIStyle(_text) { fontSize = 14, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset(6, 6, 0, 0) };
        _row.hover.textColor = Color.white;
        _button = new GUIStyle(GUI.skin.button) { font = _text.font, fontSize = 14, wordWrap = false, clipping = TextClipping.Clip };
        _button.normal.textColor = _text.normal.textColor;
        _searchStyle = new GUIStyle(GUI.skin.textField) { font = _text.font, fontSize = 16, wordWrap = false, alignment = TextAnchor.MiddleLeft };
        _panel = null;
        _buttonSprite = Menu.instance && Menu.instance.m_continueButton && Menu.instance.m_continueButton.image
            ? Menu.instance.m_continueButton.image.sprite : null;
        if (_buttonSprite)
            foreach (GUIStyleState state in (GUIStyleState[])[_button.normal, _button.hover, _button.active, _button.focused])
                state.background = null;
        if (InventoryGui.instance)
            _panel = InventoryGui.instance.m_inventoryRoot.GetComponentsInChildren<Image>(true)
                .Where(image => image.sprite && (image.name.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0
                    || image.name.IndexOf("bkg", StringComparison.OrdinalIgnoreCase) >= 0
                    || image.sprite.name.IndexOf("wood", StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(image => image.sprite).FirstOrDefault();
        UpdateColumnWidths();
    }

    private static bool Button(Rect rect, GUIContent content, bool selected = false)
    {
        if (_buttonSprite)
        {
            Color previous = GUI.color;
            GUI.color = !GUI.enabled ? new Color(0.5f, 0.5f, 0.5f) : rect.Contains(Event.current.mousePosition)
                ? new Color(1f, 0.9f, 0.7f) : Color.white;
            DrawSprite(rect, _buttonSprite!);
            GUI.color = previous;
        }
        if (selected) Fill(rect, new Color(0.7f, 0.55f, 0.18f, 0.4f));
        int originalSize = _button!.fontSize;
        try
        {
            FitFont(_button, content, rect);
            return GUI.Button(rect, content, _button);
        }
        finally { _button.fontSize = originalSize; }
    }

    private static bool DrawFilters()
    {
        Fill(new Rect(24, 108, 252, 498), new Color(0.04f, 0.055f, 0.05f, 0.92f));
        Label(new Rect(32, 116, 236, 26), "Filters", _text);
        bool changed = false;
        if (Button(new Rect(32, 150, 112, 28), new GUIContent("All", "Reset every category to Include")))
        {
            _hiddenKinds = PrefabBrowserKind.None;
            _onlyKinds = PrefabBrowserKind.None;
            changed = true;
        }
        if (Button(new Rect(152, 150, 116, 28), new GUIContent("None", "Set every category to Hide")))
        {
            _hiddenKinds = PrefabBrowserKind.All;
            _onlyKinds = PrefabBrowserKind.None;
            changed = true;
        }
        _filterScroll = GUI.BeginScrollView(new Rect(28, 190, 244, 408), _filterScroll,
            new Rect(0, 0, 224, FilterOptions.Length * 54), false, false);
        for (int index = 0; index < FilterOptions.Length; index++)
        {
            var option = FilterOptions[index];
            float top = index * 54;
            GUI.Label(new Rect(4, top, 216, 22), new GUIContent(option.Text, option.Tooltip), _row);
            int currentMode = (_hiddenKinds & option.Kind) != 0 ? 1 : (_onlyKinds & option.Kind) != 0 ? 2 : 0;
            for (int mode = 0; mode < 3; mode++)
            {
                string label = mode == 0 ? "Include" : mode == 1 ? "Hide" : "Only";
                string tooltip = mode == 0 ? $"Do not restrict {option.Text}"
                    : mode == 1 ? $"Exclude {option.Text}" : $"Show only {option.Text}; reset every other filter";
                if (!Button(new Rect(4 + mode * 72, top + 24, 70, 24), new GUIContent(label, tooltip), currentMode == mode)
                    || currentMode == mode && mode != 2) continue;
                if (mode == 2)
                {
                    _hiddenKinds = PrefabBrowserKind.None;
                    _onlyKinds = option.Kind;
                }
                else
                {
                    _hiddenKinds &= ~option.Kind;
                    _onlyKinds &= ~option.Kind;
                    if (mode == 1) _hiddenKinds |= option.Kind;
                }
                changed = true;
            }
        }
        GUI.EndScrollView();
        return changed;
    }

    private static void Label(Rect rect, string text, GUIStyle? style)
    {
        if (style == null) return;
        int originalSize = style.fontSize;
        try
        {
            GUIContent content = new(text);
            FitFont(style, content, rect);
            GUI.Label(rect, content, style);
        }
        finally { style.fontSize = originalSize; }
    }

}