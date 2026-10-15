using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.PingMark.Domain;

internal static class GroundPingLabelAnchors
{
    internal const float LabelOffset = 0.3f;
    // Mirrors the upward drift hard-coded in vanilla Chat.UpdateWorldTexts (m_position.y += dt * 0.15f).
    internal const float RiseSpeed = 0.15f;
    internal static readonly Dictionary<Chat.WorldTextInstance, Vector3> Anchors = new();
}