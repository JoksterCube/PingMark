using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.PingMark.Patches.GroundPing;

internal static class GroundPingLabelAnchors
{
    internal const float LabelOffset = 0.3f;
    internal const float RiseSpeed = 0.15f;
    internal static readonly Dictionary<Chat.WorldTextInstance, Vector3> Anchors = new();
}