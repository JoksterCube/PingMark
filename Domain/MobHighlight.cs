using System.Collections.Generic;
using JoksterCube.PingDistance.Common;
using JoksterCube.PingDistance.Settings;
using UnityEngine;
using static JoksterCube.PingDistance.Settings.PluginConfig;

namespace JoksterCube.PingDistance.Domain;

internal static class MobHighlight
{
    private const string RpcName = "JoksterCube.PingDistance.MobHighlight";
    private static readonly Dictionary<ZDOID, (float Expiration, long Order)> Highlights = new();
    private static readonly List<ZDOID> Expired = new();
    private static long _nextOrder;
    private static ZRoutedRpc? _registeredRpc;

    internal static void Update()
    {
        ZRoutedRpc? rpc = ZRoutedRpc.instance;
        if (rpc != _registeredRpc)
        {
            Highlights.Clear();
            _registeredRpc = rpc;
            if (rpc != null) rpc.Register<ZDOID>(RpcName, (_, id) => Highlight(id));
        }

        if (Enabled.Value != Toggle.On)
        {
            Highlights.Clear();
            return;
        }

        if (!Player.m_localPlayer) Highlights.Clear();
        else Trim();
    }

    internal static void Mark(Character character)
    {
        if (Enabled.Value != Toggle.On) return;

        ZDOID id = character.GetZDOID();
        if (id.IsNone()) return;

        Update();
        Highlight(id);
        ZRoutedRpc.instance?.InvokeRoutedRPC(ZRoutedRpc.Everybody, RpcName, id);
    }

    internal static bool IsMarked(Character character)
    {
        if (Enabled.Value != Toggle.On) return false;

        if (!character) return false;

        ZDOID id = character.GetZDOID();
        if (character.IsDead())
        {
            Highlights.Remove(id);
            return false;
        }
        if (Highlights.TryGetValue(id, out var highlight))
        {
            if (Time.unscaledTime < highlight.Expiration) return true;
            Highlights.Remove(id);
        }
        return false;
    }

    private static void Highlight(ZDOID id)
    {
        if (Enabled.Value == Toggle.On && !id.IsNone() && Player.m_localPlayer)
        {
            Highlights[id] = (Time.unscaledTime + MobHighlightDuration.Value, ++_nextOrder);
            Trim();
        }
    }

    private static void Trim()
    {
        foreach (KeyValuePair<ZDOID, (float Expiration, long Order)> entry in Highlights)
            if (entry.Value.Expiration <= Time.unscaledTime) Expired.Add(entry.Key);
        foreach (ZDOID id in Expired) Highlights.Remove(id);
        Expired.Clear();

        while (Highlights.Count > MaxHighlightedMobs.Value)
        {
            ZDOID oldest = ZDOID.None;
            long oldestOrder = long.MaxValue;
            foreach (KeyValuePair<ZDOID, (float Expiration, long Order)> entry in Highlights)
            {
                if (entry.Value.Order >= oldestOrder) continue;
                oldest = entry.Key;
                oldestOrder = entry.Value.Order;
            }
            Highlights.Remove(oldest);
        }
    }
}