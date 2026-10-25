namespace JoksterCube.PingMark.Domain.Enums;

internal enum FatigueMode
{
    // Always Perception Fatigue Duration.
    Fixed = 0,
    // Perception Fatigue Duration plus a cost for everything Perception found.
    Relative = 1
}
