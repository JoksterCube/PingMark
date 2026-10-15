using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Common.Networking;
using JoksterCube.PingMark.Domain.Models;
using UnityEngine;
using static JoksterCube.PingMark.Domain.Collections.PrefabCollectionEditorUI;
using static JoksterCube.PingMark.Domain.Collections.PrefabEditorStyles;
using static JoksterCube.PingMark.Settings.PluginConfig;
using SectionColorPalette = JoksterCube.PingMark.Settings.Constants.Groups.PrefabCollections;

namespace JoksterCube.PingMark.Domain.Collections;

internal static class PrefabCollectionEditor
{
    private static readonly HashSet<string> Selected = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<Vector2> Scroll = [];
    private static readonly List<string[]> Visible = [];
    private static readonly List<float> ColumnWidths = [];
    private static PrefabCollections? _collections;
    private static List<PrefabCollectionSection> _sections = [];
    private static IEnumerator<int>? _loading;
    private static string[]? _cachedNames;
    private static int _openedFrame;
    private static PrefabBrowserFilter _filter = new();
    private static string _original = string.Empty;
    private static string _initialStaged = string.Empty;
    private static string _staged = string.Empty;
    private static bool _stagedDirty;
    private static readonly Dictionary<string, float> RowWidths = new(StringComparer.Ordinal);
    private static string _search = string.Empty;
    private static string _status = string.Empty;
    private static int _openColorPicker = -1;
    private static Vector2 _collectionScroll;
    private static int _unassignedColorId;
    private static int _initialUnassignedColorId;
    private static bool _showClosePrompt;
    private static bool _showAutoSortPrompt;
    private static readonly Stack<(PrefabCollections Collections, List<PrefabCollectionSection> Sections)> SnapshotUndo = new();
    private static int _closedFrame = -1;

    internal static Behaviour? View { get; set; }

    internal static bool IsOpen => _collections != null;
    internal static bool BlocksInput => IsOpen || _closedFrame == Time.frameCount;
    internal static bool CanEdit => !RpcHandlers.ShouldWaitForInitialConfigSync && PrefabCollectionStore.IsReady && Plugin.CanEditPrefabCollections;

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
        PrefabEditorStyles.Reset();
        Selected.Clear();
        _openColorPicker = -1;
        _showClosePrompt = false;
        _showAutoSortPrompt = false;
        SnapshotUndo.Clear();
        LoadHeaderColors();
        _collectionScroll = Vector2.zero;
        ResetColumnState();
        Refresh();
        _openedFrame = Time.frameCount;
        _loading = _cachedNames == null ? LoadCollections().GetEnumerator() : null;
        if (View) View!.enabled = true;
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
        SnapshotUndo.Clear();
        _closedFrame = Time.frameCount;
        if (View) View!.enabled = false;
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
        Refresh(updateWidths: false);
        for (int index = 0; index < _collections!.CollectionCount; index++)
        {
            ColumnWidths[index] = DefaultColumnWidth;
            foreach (string name in Visible[index])
            {
                if (RowStyle != null)
                    ColumnWidths[index] = Mathf.Max(ColumnWidths[index], RowWidth(name));
                yield return names.Length;
            }
        }
        _cachedNames = names;
    }

    internal static void Draw(Plugin plugin)
    {
        if (!IsOpen) return;
        if (PrefabEditorStyles.Ensure())
        {
            RowWidths.Clear();
            UpdateColumnWidths();
        }
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
            DrawPanel(new Rect(0, 0, CanvasWidth, CanvasHeight), PanelSprite);
            Label(new Rect(24, 16, 800, 38), "PingMark | Prefab Collections", TitleStyle);
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
            Label(new Rect(24, 64, 100, 32), "Search", TextStyle);
            GUI.SetNextControlName("PingMarkPrefabSearch");
            string search = GUI.TextField(new Rect(134, 64, 800, 32), _search, SearchStyle);
            if (search != _search)
            {
                _search = search;
                Selected.Clear();
                ResetScrollPositions();
                Refresh(contentChanged: false);
            }
            if (Button(new Rect(946, 64, 230, 32), new GUIContent("Select Visible")))
                foreach (string[] column in Visible)
                    foreach (string name in column) Selected.Add(name);

            if (PrefabEditorFilters.Draw())
            {
                Selected.Clear();
                ResetScrollPositions();
                Refresh(contentChanged: false);
            }

            DrawColumn(0, 292, true);
            float sectionContentWidth = Mathf.Max(MinimumSectionContentWidth,
                _sections.Count * SectionColumnStride - SectionContentPadding);
            _collectionScroll = GUI.BeginScrollView(new Rect(514, 108, 662, 498), _collectionScroll,
                new Rect(0, 0, sectionContentWidth, 498), false, false);
            for (int index = 1; index < _collections!.CollectionCount; index++)
                DrawColumn(index, (index - 1) * SectionColumnStride, false);
            GUI.EndScrollView();

            Label(new Rect(24, 618, 174, 32), $"Selected: {Selected.Count}", TextStyle);
            GUI.enabled = previousEnabled && _loading == null && Selected.Count > 0 && !_showClosePrompt && !_showAutoSortPrompt;
            if (Button(new Rect(210, 618, 198, 32), new GUIContent("Unselect all", "Clear selection without moving any prefabs")))
                Selected.Clear();
            GUI.enabled = previousEnabled && !_showClosePrompt && !_showAutoSortPrompt;
            Label(new Rect(24, 656, 1152, 26), _status.Length > 0 ? _status : GUI.tooltip, TextStyle);
            GUI.enabled = previousEnabled && _loading == null && (_collections?.CanUndo == true || SnapshotUndo.Count > 0)
                && !_showClosePrompt && !_showAutoSortPrompt;
            if (Button(new Rect(708, 690, 156, 34), new GUIContent("Undo", "Undo the last move, removal or auto sort")))
            {
                if (_collections?.Undo() != true && SnapshotUndo.Count > 0)
                {
                    (_collections, _sections) = SnapshotUndo.Pop();
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
            Label(new Rect(left + 8, 116 + yOffset, 162, 26), "Unassigned", TextStyle);
        else
        {
            PrefabCollectionSection section = _sections[index - 1];
            string name = GUI.TextField(new Rect(left + 8, 116 + yOffset, 132, 26), section.Name, SearchStyle);
            if (name != section.Name)
            {
                section.Name = name;
                _stagedDirty = true;
            }
            bool enabled = GUI.Toggle(new Rect(left + 8, 142 + yOffset, 20, 18), section.Enabled,
                new GUIContent(string.Empty, $"Enable {section.Name}"));
            if (enabled != section.Enabled)
            {
                section.Enabled = enabled;
                _stagedDirty = true;
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
            // Removal shifts section indexes and clears move history, so undo it via a full snapshot.
            SnapshotUndo.Push((_collections.Clone(), _sections.Select(section => section.Copy()).ToList()));
            _sections.RemoveAt(index - 1);
            _collections.RemoveSection(index);
            Selected.Clear();
            _openColorPicker = -1;
            Refresh();
            return;
        }
        string[] names = Visible[index];
        Label(new Rect(left + (unassigned ? 8 : 32), 142 + yOffset, unassigned ? 198 : 174, 18),
            $"{names.Length:N0} / {_collections.Count(index):N0} prefabs", CountStyle);
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
            if (GUI.Button(rect, new GUIContent(name, $"{name} | {_filter.Kind(name)}"), RowStyle))
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
        SnapshotUndo.Push((_collections, _sections));
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
        DrawPanel(panel, PanelSprite);
        Label(new Rect(panel.x + 24, panel.y + 16, panel.width - 48, 34), "Replace all sections?", TitleStyle);
        Label(new Rect(panel.x + 24, panel.y + 60, panel.width - 48, 30),
            "All sections will be replaced with Valuables, Locations and Useful.", TextStyle);
        Label(new Rect(panel.x + 24, panel.y + 94, panel.width - 48, 30),
            "Custom names, colors and enabled states will be reset.", TextStyle);
        Label(new Rect(panel.x + 24, panel.y + 128, panel.width - 48, 30),
            "Unknown/excluded IDs return to Unassigned. Apply saves changes.", TextStyle);
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
        Label(new Rect(panel.x + 20, panel.y + 16, panel.width - 40, 34), "Apply pending changes?", TitleStyle);
        Label(new Rect(panel.x + 20, panel.y + 58, panel.width - 40, 30),
            "Your prefab collections or section colors have changed.", TextStyle);
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
                    _stagedDirty = true;
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

    private static bool HasPendingChanges() => _initialStaged != Staged || _unassignedColorId != _initialUnassignedColorId;

    // Serialized lazily: HasPendingChanges runs for every IMGUI event, mutations only flag the change.
    private static string Staged
    {
        get
        {
            if (_stagedDirty)
            {
                _staged = StagedConfig();
                _stagedDirty = false;
            }
            return _staged;
        }
    }

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
        StagedConfig();
        bool saveOnSet = plugin.Config.SaveOnConfigSet;
        try
        {
            plugin.Config.SaveOnConfigSet = false;
            PrefabCollectionSectionConfig.Save(_sections);
            SaveHeaderColors();
            // ServerSync keeps server-provided values out of the local file, so saving is safe for remote admins
            // and persists the local-only Unassigned color.
            plugin.Config.Save();
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

    private static string CurrentConfig() => PrefabCollectionStore.Value;

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

    private static void Refresh(bool contentChanged = true, bool updateWidths = true)
    {
        if (_collections == null) return;
        EnsureColumnState();
        for (int index = 0; index < _collections.CollectionCount; index++)
            Visible[index] = _collections.Names(index, _search).Where(name => PrefabEditorFilters.Includes(_filter, name)).ToArray();
        if (contentChanged) _stagedDirty = true;
        if (updateWidths) UpdateColumnWidths();
    }

    private static void UpdateColumnWidths()
    {
        if (RowStyle == null) return;
        for (int index = 0; index < _collections!.CollectionCount; index++)
        {
            float width = DefaultColumnWidth;
            foreach (string name in Visible[index])
                width = Mathf.Max(width, RowWidth(name));
            ColumnWidths[index] = width;
        }
    }

    private static float RowWidth(string name)
    {
        if (!RowWidths.TryGetValue(name, out float width))
            RowWidths[name] = width = RowStyle!.CalcSize(new GUIContent(name)).x + ColumnTextPadding;
        return width;
    }

}