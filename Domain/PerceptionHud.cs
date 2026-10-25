using JoksterCube.PingMark.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

// A copy of the Guardian Power slot that shows Perception: its name, Ready or the time until it can be used again.
// It sits right of the Guardian Power, or in its place while no power is chosen.
internal static class PerceptionHud
{
    private const string Name = "Perception";
    // Vanilla's look for a power on cooldown.
    private static readonly Color CooldownColor = new(1f, 0f, 1f, 0f);
    // Space kept between the two icons, and between the two names when those are wider.
    private const float IconGap = 10f;
    private const float NameGap = 8f;

    private static RectTransform? _root;
    private static TMP_Text? _name;
    private static TMP_Text? _cooldown;
    private static Image? _icon;

    internal static void Update(Hud hud)
    {
        bool show = Enabled.IsOn() && PerceptionEnabled.IsOn();
        // The copy is destroyed with the Hud on logout and made again for the next one.
        if (!_root)
        {
            if (!show) return;
            Create(hud);
        }
        if (_root!.gameObject.activeSelf != show) _root.gameObject.SetActive(show);
        if (!show) return;

        RectTransform power = hud.m_gpRoot;
        Vector2 position = power.anchoredPosition;
        if (power.gameObject.activeSelf) position.x += Spacing(hud);
        _root.anchoredPosition = position;

        float cooldown = Perception.Cooldown;
        _icon!.sprite = Perception.Icon;
        _icon.color = cooldown <= 0f ? Color.white : CooldownColor;
        _cooldown!.text = cooldown > 0f ? StatusEffect.GetTimeString(cooldown) : Localization.instance.Localize("$hud_ready");
    }

    // An icon apart, or further when long names such as Bonemass would run into Perception.
    private static float Spacing(Hud hud)
    {
        Transform parent = hud.m_gpRoot.parent;
        float icons = Width(hud.m_gpIcon.rectTransform, parent) + IconGap;
        float names = (Width(hud.m_gpName, parent) + Width(_name!, parent)) / 2f + NameGap;
        return Mathf.Max(icons, names);
    }

    // Widths as the slots' parent measures them.
    private static float Width(TMP_Text text, Transform parent) => Scaled(text.preferredWidth, text.transform, parent);

    private static float Width(RectTransform rect, Transform parent) => Scaled(rect.rect.width, rect, parent);

    private static float Scaled(float width, Transform transform, Transform parent) =>
        width * transform.lossyScale.x / Mathf.Max(parent.lossyScale.x, 0.0001f);

    private static void Create(Hud hud)
    {
        RectTransform power = hud.m_gpRoot;
        GameObject copy = Object.Instantiate(power.gameObject, power.parent);
        copy.name = "PingMark_Perception";
        _root = (RectTransform)copy.transform;
        _name = Find(power, hud.m_gpName, _root);
        _cooldown = Find(power, hud.m_gpCooldown, _root);
        _icon = Find(power, hud.m_gpIcon, _root);
        _name.text = Name;
        // The power's touch button would start the Guardian Power; Perception has its own shortcut.
        foreach (UIInputHandler handler in copy.GetComponentsInChildren<UIInputHandler>(true))
            handler.enabled = false;
    }

    // The same child in the copy, found by its path under the original root.
    private static T Find<T>(Transform root, T original, Transform copy) where T : Component
    {
        string path = "";
        for (Transform current = original.transform; current != root; current = current.parent)
            path = path.Length == 0 ? current.name : current.name + "/" + path;
        return path.Length == 0 ? copy.GetComponent<T>() : copy.Find(path).GetComponent<T>();
    }
}
