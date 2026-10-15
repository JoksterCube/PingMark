using System.Collections.Generic;
using UnityEngine;

namespace JoksterCube.PingMark.Domain.Networking;

// Server-side token bucket per sender: short bursts are fine, sustained spam is dropped.
internal static class PingRateLimiter
{
    private const float Capacity = 10f;
    private const float RefillPerSecond = 4f;

    private static readonly Dictionary<long, (float Tokens, float Time)> Buckets = [];

    internal static bool TryConsume(long senderId)
    {
        float now = Time.realtimeSinceStartup;
        (float tokens, float time) = Buckets.TryGetValue(senderId, out var state) ? state : (Capacity, now);
        tokens = Mathf.Min(Capacity, tokens + (now - time) * RefillPerSecond);
        bool allowed = tokens >= 1f;
        Buckets[senderId] = (allowed ? tokens - 1f : tokens, now);
        return allowed;
    }

    internal static void Forget(long senderId) => Buckets.Remove(senderId);

    internal static void Clear() => Buckets.Clear();
}
