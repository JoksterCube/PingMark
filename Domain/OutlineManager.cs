using System.Collections.Generic;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain.Models;
using UnityEngine;
using UnityEngine.Rendering;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

internal static class OutlineManager
{
    private const CameraEvent RenderEvent = CameraEvent.AfterForwardAlpha;
    private const int StencilBit = 64;
    private const float Diagonal = 0.70710678f;

    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private static readonly AccessTools.FieldRef<EnemyHud, Dictionary<Character, EnemyHud.HudData>> HudsRef =
        AccessTools.FieldRefAccess<EnemyHud, Dictionary<Character, EnemyHud.HudData>>("m_huds");

    private static readonly Vector2[] Directions =
    [
        new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f),
        new(Diagonal, Diagonal), new(-Diagonal, Diagonal), new(Diagonal, -Diagonal), new(-Diagonal, -Diagonal)
    ];

    private static readonly List<ZDOID> MarkedIds = [];
    private static readonly Dictionary<GameObject, RendererCache> RendererCaches = [];
    private static readonly HashSet<GameObject> ActiveOutlineTargets = [];
    private static readonly List<GameObject> ExpiredOutlineTargets = [];
    private static readonly HashSet<Renderer> CollectedRenderers = [];
    private static readonly List<(Renderer Renderer, int SubMeshCount)> Targets = [];
    private static readonly Dictionary<Color32, Material> OutlineMaterials = [];
    private static readonly Dictionary<Chat.WorldTextInstance, PingTarget> PingTargets = [];
    private static readonly List<Chat.WorldTextInstance> ExpiredPings = [];
    private static readonly HashSet<Chat.WorldTextInstance> ExistingPings = [];

    private static CommandBuffer? _buffer;
    private static Camera? _camera;
    private static Shader? _shader;
    private static Material? _clearMaterial;
    private static Material? _maskMaterial;
    private static bool _shaderMissing;

    private sealed class RendererCache
    {
        internal readonly List<Renderer> Renderers = [];
        internal readonly List<Transform> Transforms = [];
        internal readonly List<int> ChildCounts = [];
        internal float NextRefresh;
    }

    internal static void Initialize() => Camera.onPreRender += OnPreRender;

    internal static void SetPingTarget(Chat.WorldTextInstance ping, PingTarget? target)
    {
        if (target.HasValue) PingTargets[ping] = target.Value;
        else PingTargets.Remove(ping);
    }

    internal static bool TryGetPingColor(Chat.WorldTextInstance ping, out Color color)
    {
        bool found = PingTargets.TryGetValue(ping, out PingTarget target);
        color = found ? target.Color : default;
        return found;
    }

    internal static void UpdatePings(List<Chat.WorldTextInstance> worldTexts)
    {
        if (!Enabled.IsOn())
        {
            PingTargets.Clear();
            return;
        }
        if (PingTargets.Count == 0) return;
        ExistingPings.Clear();
        foreach (Chat.WorldTextInstance ping in worldTexts) ExistingPings.Add(ping);

        // Entries outlive their target so the label keeps its color after the object is destroyed.
        foreach (KeyValuePair<Chat.WorldTextInstance, PingTarget> entry in PingTargets)
            if (!entry.Key.m_gui || entry.Key.m_type != Talker.Type.Ping || !ExistingPings.Contains(entry.Key))
                ExpiredPings.Add(entry.Key);
        foreach (Chat.WorldTextInstance ping in ExpiredPings)
            PingTargets.Remove(ping);
        ExpiredPings.Clear();
        ExistingPings.Clear();
    }

    internal static void Dispose()
    {
        Camera.onPreRender -= OnPreRender;
        Detach();
        _buffer?.Release();
        _buffer = null;
        if (_clearMaterial) Object.Destroy(_clearMaterial);
        if (_maskMaterial) Object.Destroy(_maskMaterial);
        foreach (Material material in OutlineMaterials.Values)
            if (material) Object.Destroy(material);
        OutlineMaterials.Clear();
        PingTargets.Clear();
        ExpiredPings.Clear();
        ExistingPings.Clear();
        RendererCaches.Clear();
        ActiveOutlineTargets.Clear();
        ExpiredOutlineTargets.Clear();
    }

    private static void OnPreRender(Camera camera)
    {
        GameCamera gameCamera = GameCamera.instance;
        if (!gameCamera || camera.gameObject != gameCamera.gameObject) return;

        if (camera != _camera) Attach(camera);
        _buffer!.Clear();
        ActiveOutlineTargets.Clear();

        if (!Enabled.IsOn() || OutlineWidth.Value <= 0f)
        {
            RendererCaches.Clear();
            return;
        }

        MarkedIds.Clear();
        if ((MobOutlineEnabled.IsOn() || (PlayersEnabled.IsOn() && PingOutlineEnabled.IsOn())) && ZNetScene.instance)
            MobHighlight.GetMarkedIds(MarkedIds);
        bool drawPings = PingOutlineEnabled.IsOn() && PingTargets.Count > 0;
        if ((MarkedIds.Count == 0 && !drawPings) || !EnsureMaterials())
        {
            RendererCaches.Clear();
            return;
        }

        Matrix4x4 view = camera.worldToCameraMatrix;
        Matrix4x4 projection = camera.projectionMatrix;
        Vector2 pixel = new(2f * OutlineWidth.Value / camera.pixelWidth, 2f * OutlineWidth.Value / camera.pixelHeight);

        if (drawPings && Chat.instance)
        {
            foreach (KeyValuePair<Chat.WorldTextInstance, PingTarget> entry in PingTargets)
            {
                if (!entry.Key.m_gui || entry.Key.m_type != Talker.Type.Ping) continue;
                Targets.Clear();
                CollectedRenderers.Clear();
                foreach (GameObject target in entry.Value.OutlineTargets)
                    if (target) CollectTargets(target);
                DrawCollectedOutline(entry.Value.Color, view, projection, pixel);
            }
        }

        foreach (ZDOID id in MarkedIds)
        {
            GameObject instance = ZNetScene.instance!.FindInstance(id);
            if (!instance || !instance.TryGetComponent(out Character character) || !MobHighlight.ShouldDrawOutline(character)) continue;

            Color color = character.IsPlayer() ? PingPlayerColor.Value : GetMobColor(character);
            DrawOutline(character.gameObject, color, view, projection, pixel);
        }

        _buffer!.SetViewProjectionMatrices(view, projection);
        foreach (GameObject target in RendererCaches.Keys)
            if (!target || !ActiveOutlineTargets.Contains(target)) ExpiredOutlineTargets.Add(target!);
        foreach (GameObject target in ExpiredOutlineTargets) RendererCaches.Remove(target);
        ExpiredOutlineTargets.Clear();
    }

    private static void DrawOutline(GameObject target, Color color, Matrix4x4 view, Matrix4x4 projection, Vector2 pixel)
    {
        Targets.Clear();
        CollectedRenderers.Clear();
        CollectTargets(target);
        DrawCollectedOutline(color, view, projection, pixel);
    }

    private static void DrawCollectedOutline(Color color, Matrix4x4 view, Matrix4x4 projection, Vector2 pixel)
    {
        if (Targets.Count == 0) return;

        DrawOffsets(_clearMaterial!, view, projection, pixel);
        _buffer!.SetViewProjectionMatrices(view, projection);
        DrawTargets(_maskMaterial!);
        DrawOffsets(GetOutlineMaterial(color), view, projection, pixel);
    }

    private static void Attach(Camera camera)
    {
        Detach();
        _buffer ??= new CommandBuffer { name = "PingMark Outline" };
        camera.AddCommandBuffer(RenderEvent, _buffer);
        _camera = camera;
    }

    private static void Detach()
    {
        if (_camera && _buffer != null) _camera!.RemoveCommandBuffer(RenderEvent, _buffer);
        _camera = null;
    }

    private static void CollectTargets(GameObject target)
    {
        ActiveOutlineTargets.Add(target);
        RendererCache cache = GetRendererCache(target);
        foreach (Renderer renderer in cache.Renderers)
        {
            if (!renderer)
            {
                cache.NextRefresh = 0f;
                continue;
            }
            if (!renderer.gameObject.activeInHierarchy || !renderer.enabled
                || !renderer.isVisible || !CollectedRenderers.Add(renderer)) continue;

            Mesh? mesh = null;
            if (renderer is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
            else if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
            if (mesh) Targets.Add((renderer, mesh!.subMeshCount));
        }
    }

    private static RendererCache GetRendererCache(GameObject target)
    {
        if (!RendererCaches.TryGetValue(target, out RendererCache cache))
            RendererCaches[target] = cache = new RendererCache();

        bool refresh = cache.Transforms.Count == 0 || Time.unscaledTime >= cache.NextRefresh;
        if (!refresh)
            for (int index = 0; index < cache.Transforms.Count; index++)
                if (!cache.Transforms[index] || cache.Transforms[index].childCount != cache.ChildCounts[index])
                {
                    refresh = true;
                    break;
                }
        if (!refresh) return cache;

        cache.Renderers.Clear();
        cache.Transforms.Clear();
        cache.ChildCounts.Clear();
        target.GetComponentsInChildren(true, cache.Renderers);
        target.GetComponentsInChildren(true, cache.Transforms);
        foreach (Transform transform in cache.Transforms) cache.ChildCounts.Add(transform.childCount);
        cache.NextRefresh = Time.unscaledTime + 1f;
        return cache;
    }

    private static void DrawOffsets(Material material, Matrix4x4 view, Matrix4x4 projection, Vector2 pixel)
    {
        foreach (Vector2 direction in Directions)
        {
            Matrix4x4 offset = Matrix4x4.Translate(new Vector3(direction.x * pixel.x, direction.y * pixel.y, 0f));
            _buffer!.SetViewProjectionMatrices(view, offset * projection);
            DrawTargets(material);
        }
    }

    private static void DrawTargets(Material material)
    {
        foreach ((Renderer renderer, int subMeshCount) in Targets)
            for (int i = 0; i < subMeshCount; i++)
                _buffer!.DrawRenderer(renderer, material, i);
    }

    private static Color GetMobColor(Character character)
    {
        bool ally = character.IsTamed();
        EnemyHud hud = EnemyHud.instance;
        if (hud && HudsRef(hud).TryGetValue(character, out EnemyHud.HudData data))
            ally = ally || (data.m_healthFastFriendly && data.m_healthFastFriendly.gameObject.activeSelf);
        return ally ? AllyOutlineColor.Value : HostileOutlineColor.Value;
    }

    private static bool EnsureMaterials()
    {
        if (_clearMaterial && _maskMaterial) return true;
        if (_shaderMissing) return false;

        // UI/Default ships with every Unity build and exposes stencil, color mask and ZTest overrides.
        _shader ??= Shader.Find("UI/Default");
        if (!_shader)
        {
            _shaderMissing = true;
            Debug.LogWarning("[PingMark] UI/Default shader not found, outlines are disabled.");
            return false;
        }

        _clearMaterial = CreateMaterial(CompareFunction.Always, StencilOp.Zero, false, Color.clear);
        _maskMaterial = CreateMaterial(CompareFunction.Always, StencilOp.Replace, false, Color.clear);
        return true;
    }

    private static Material GetOutlineMaterial(Color color)
    {
        color.a = 1f;
        Color32 key = color;
        if (!OutlineMaterials.TryGetValue(key, out Material material) || !material)
            OutlineMaterials[key] = material = CreateMaterial(CompareFunction.NotEqual, StencilOp.Replace, true, color);
        return material;
    }

    private static Material CreateMaterial(CompareFunction comparison, StencilOp operation, bool writeColor, Color color)
    {
        Material material = new(_shader) { hideFlags = HideFlags.HideAndDontSave };
        material.SetColor(ColorId, color);
        material.SetFloat("_Stencil", StencilBit);
        material.SetFloat("_StencilComp", (float)comparison);
        material.SetFloat("_StencilOp", (float)operation);
        material.SetFloat("_StencilReadMask", StencilBit);
        material.SetFloat("_StencilWriteMask", StencilBit);
        material.SetFloat("_ColorMask", writeColor ? (float)ColorWriteMask.All : 0f);
        material.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
        return material;
    }
}