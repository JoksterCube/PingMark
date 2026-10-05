using System.Collections.Generic;

namespace JoksterCube.PingMark.Domain.Models;

internal sealed class SerializedSections
{
    public SerializedSections() { }
    public int Version { get; set; } = 1;
    public List<SerializedSection> Sections { get; set; } = [];
}