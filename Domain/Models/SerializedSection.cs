using static JoksterCube.PingMark.Settings.Constants.Groups.PrefabCollections;

namespace JoksterCube.PingMark.Domain.Models;

internal sealed class SerializedSection
{
    public SerializedSection() { }
    public string Name { get; set; } = string.Empty;
    public string Prefabs { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int ColorId { get; set; } = Color1Id;
}