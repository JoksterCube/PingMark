using System.Text;
using System.Text.RegularExpressions;
using JoksterCube.PingMark.Domain.Enums;
using static JoksterCube.PingMark.Settings.PluginConfig;
using UnityEngine;

namespace JoksterCube.PingMark.Domain.Models;

internal readonly struct PingTarget(PingCategory category, string name, GameObject? outlineTarget, Color? colorOverride = null)
{
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    internal PingCategory Category { get; } = category;
    internal string Name { get; } = FormatName(name);
    internal GameObject[] OutlineTargets { get; } = outlineTarget ? [outlineTarget!] : [];

    private static string FormatName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        StringBuilder builder = new(name.Length);
        foreach (char c in name)
        {
            if (char.IsDigit(c)) continue;
            char formatted = c == '_' ? ' ' : c;
            if (char.IsUpper(formatted) && builder.Length > 0 && char.IsLower(builder[builder.Length - 1]))
                builder.Append(' ');
            builder.Append(formatted);
        }
        return Spaces.Replace(builder.ToString(), " ").Trim();
    }

    internal Color Color => colorOverride ?? (Category switch
    {
        PingCategory.PrefabSection => PingAnythingColor.Value,
        PingCategory.Drop => PingDropColor.Value,
        PingCategory.Gravestone or PingCategory.Player => PingPlayerColor.Value,
        _ => PingAnythingColor.Value
    });
}