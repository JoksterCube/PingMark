using JoksterCube.PingMark.Domain.Enums;
using static JoksterCube.PingMark.Settings.PluginConfig;
using UnityEngine;

namespace JoksterCube.PingMark.Domain.Models;

internal readonly struct PingTarget(PingCategory category, string name, GameObject? outlineTarget, Color? colorOverride = null)
{
    internal PingCategory Category { get; } = category;
    internal string Name { get; } = TargetNameFormatter.Format(name);
    internal GameObject[] OutlineTargets { get; } = outlineTarget ? [outlineTarget!] : [];

    internal Color Color => colorOverride ?? (Category switch
    {
        PingCategory.PrefabSection => PingAnythingColor.Value,
        PingCategory.Drop => PingDropColor.Value,
        PingCategory.Fish => PingFishColor.Value,
        PingCategory.Gravestone or PingCategory.Player => PingPlayerColor.Value,
        _ => PingAnythingColor.Value
    });
}