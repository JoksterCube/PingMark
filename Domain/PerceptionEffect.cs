using JoksterCube.PingMark.Common;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

// The burst a Guardian Power makes on activation, recolored. The prefab is networked, so a shared copy would
// reach everyone in its original colors; each PingMark client plays its own local copy instead.
internal static class PerceptionEffect
{
    private const string PrefabName = "fx_GP_Activation";
    private const float FallbackLifetime = 10f;
    private static readonly string[] MaterialColors = ["_Color", "_TintColor", "_EmissionColor"];

    internal static void Play(Player player)
    {
        if (!PerceptionEffectEnabled.IsOn() || !player || !ZNetScene.instance) return;
        GameObject prefab = ZNetScene.instance.GetPrefab(PrefabName);
        if (!prefab) return;

        // Attached like a Guardian Power's effect, so it stays with a running player.
        GameObject effect = SpawnLocal(prefab, player);
        // The Guardian Power blast is too loud for a sense; its sounds start on their first frame, so they never play.
        foreach (ZSFX sound in effect.GetComponentsInChildren<ZSFX>(true))
            if (sound.gameObject != effect) sound.gameObject.SetActive(false);
        Recolor(effect, PerceptionEffectColor.Value);

        string soundName = PerceptionEffectSound.Value.Trim();
        if (soundName.Length > 0 && ZNetScene.instance.GetPrefab(soundName) is { } soundPrefab && soundPrefab)
            SpawnLocal(soundPrefab, player);
    }

    private static GameObject SpawnLocal(GameObject prefab, Player player)
    {
        GameObject spawned;
        ZNetView.m_forceDisableInit = true;
        try
        {
            spawned = Object.Instantiate(prefab, player.GetCenterPoint(), Quaternion.identity, player.transform);
        }
        finally
        {
            ZNetView.m_forceDisableInit = false;
        }
        // TimedDestruction only removes networked copies, so the local copy removes itself.
        float lifetime = spawned.TryGetComponent(out TimedDestruction timed) ? timed.m_timeout : FallbackLifetime;
        Object.Destroy(spawned, lifetime);
        return spawned;
    }

    private static void Recolor(GameObject effect, Color color)
    {
        foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            // Restarted so the first burst is already emitted in the new color.
            system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.startColor = Recolor(main.startColor, color);
            ParticleSystem.ColorOverLifetimeModule overLifetime = system.colorOverLifetime;
            if (overLifetime.enabled) overLifetime.color = Recolor(overLifetime.color, color);
            ParticleSystem.TrailModule trails = system.trails;
            if (trails.enabled) trails.colorOverLifetime = Recolor(trails.colorOverLifetime, color);
            if (system.TryGetComponent(out ParticleSystemRenderer renderer)) RecolorMaterials(renderer, color);
            system.Play(false);
        }
        foreach (Light light in effect.GetComponentsInChildren<Light>(true))
            light.color = Recolor(light.color, color);
    }

    private static void RecolorMaterials(Renderer renderer, Color color)
    {
        // Instanced materials, so the shared prefab materials keep their colors.
        foreach (Material material in renderer.materials)
            foreach (string property in MaterialColors)
                if (material.HasProperty(property)) material.SetColor(property, Recolor(material.GetColor(property), color));
    }

    private static ParticleSystem.MinMaxGradient Recolor(ParticleSystem.MinMaxGradient gradient, Color color) =>
        gradient.mode switch
        {
            ParticleSystemGradientMode.Color => new ParticleSystem.MinMaxGradient(Recolor(gradient.color, color)),
            ParticleSystemGradientMode.TwoColors => new ParticleSystem.MinMaxGradient(
                Recolor(gradient.colorMin, color), Recolor(gradient.colorMax, color)),
            ParticleSystemGradientMode.Gradient => new ParticleSystem.MinMaxGradient(Recolor(gradient.gradient, color)),
            ParticleSystemGradientMode.TwoGradients => new ParticleSystem.MinMaxGradient(
                Recolor(gradient.gradientMin, color), Recolor(gradient.gradientMax, color)),
            _ => new ParticleSystem.MinMaxGradient(color)
        };

    private static Gradient Recolor(Gradient gradient, Color color)
    {
        GradientColorKey[] keys = gradient.colorKeys;
        for (int index = 0; index < keys.Length; index++) keys[index].color = Recolor(keys[index].color, color);
        Gradient result = new() { mode = gradient.mode };
        result.SetKeys(keys, gradient.alphaKeys);
        return result;
    }

    // The new hue at the original brightness and transparency, so glows stay glows and fades stay fades.
    private static Color Recolor(Color original, Color color)
    {
        float brightness = Mathf.Max(original.r, original.g, original.b);
        float scale = brightness / Mathf.Max(color.r, color.g, color.b, 0.0001f);
        return new Color(color.r * scale, color.g * scale, color.b * scale, original.a);
    }
}
