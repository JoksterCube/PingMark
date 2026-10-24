using System.Text;
using System.Text.RegularExpressions;

namespace JoksterCube.PingMark.Domain.Models;

internal static class TargetNameFormatter
{
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    // Prefab-style IDs ("MineRock_Copper2") become readable ("Mine Rock Copper"); display names that
    // already contain spaces are kept as they are, because their digits may be meaningful.
    internal static string Format(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        name = name!.Trim();
        if (name.IndexOf(' ') >= 0) return Spaces.Replace(name, " ");

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
}
