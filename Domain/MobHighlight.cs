using System.Collections.Generic;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain.Networking;
using JoksterCube.PingMark.Domain.PingTargets;
using JoksterCube.PingMark.Settings;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

internal static class MobHighlight
{
    private static readonly Dictionary<long, Dictionary<ZDOID, (float Expiration, long Order, UserInfo User, bool IsPlayer)>> Highlights = [];
    private static readonly Dictionary<ZDOID, (float Expiration, long Order, UserInfo User, bool IsPlayer)> ActiveHighlights = [];
    private static readonly List<ZDOID> Expired = [];
    private static readonly List<long> ExpiredPlayers = [];
    private static long _nextOrder;
    private static ZRoutedRpc? _registeredRpc;
    private static int _indexedFrame = -1;
    private static int _indexedLimit;
    private static bool _indexedMobsEnabled;
    private static bool _indexedPlayersEnabled;

    internal static void Update()
    {
        ZRoutedRpc? rpc = ZRoutedRpc.instance;
        if (rpc != _registeredRpc)
        {
            Highlights.Clear();
            ActiveHighlights.Clear();
            _indexedFrame = -1;
            _registeredRpc = rpc;
        }

        if (!Enabled.IsOn() || !Player.m_localPlayer || (!MobHighlightEnabled.IsOn() && !PlayersEnabled.IsOn()))
        {
            Highlights.Clear();
            ActiveHighlights.Clear();
            _indexedFrame = -1;
            return;
        }

        if (_indexedFrame != Time.frameCount || _indexedLimit != MaxHighlightedMobs.Value
            || _indexedMobsEnabled != MobHighlightEnabled.IsOn() || _indexedPlayersEnabled != PlayersEnabled.IsOn())
            Trim();
    }

    internal static void Mark(Character character)
    {
        if (!Enabled.IsOn() || !character || character.IsDead()
            || !(character.IsPlayer() ? PlayersEnabled.IsOn() : MobHighlightEnabled.IsOn())) return;

        ZDOID id = character.GetZDOID();
        if (id.IsNone()) return;

        Update();
        if (character.IsPlayer()) ZPingNetwork.SendPlayer(id, character.transform.position);
        else ZPingNetwork.SendMob(id, character.transform.position);
    }

    internal static bool IsMarked(Character character)
    {
        if (!Enabled.IsOn() || !character
            || !(character.IsPlayer() ? PlayersEnabled.IsOn() : MobHighlightEnabled.IsOn())) return false;

        Update();
        ZDOID id = character.GetZDOID();
        if (character.IsDead())
        {
            foreach (var highlights in Highlights.Values) highlights.Remove(id);
            ActiveHighlights.Remove(id);
            return false;
        }
        return ActiveHighlights.ContainsKey(id);
    }

    internal static void GetMarkedIds(List<ZDOID> ids)
    {
        Update();
        ids.Clear();
        ids.AddRange(ActiveHighlights.Keys);
    }

    internal static bool ShouldDrawOutline(Character character) =>
        IsMarked(character) && (character.IsPlayer()
            ? PingOutlineEnabled.IsOn() && (!HideOwnPlayerOutline.IsOn() || character != Player.m_localPlayer)
            : MobOutlineEnabled.IsOn());

    internal static string GetPingerName(Character character)
    {
        Update();
        return ActiveHighlights.TryGetValue(character.GetZDOID(), out var highlight)
            ? highlight.User.GetDisplayName()
            : string.Empty;
    }

    internal static long GetPingerOrder(Character character)
    {
        Update();
        return ActiveHighlights.TryGetValue(character.GetZDOID(), out var highlight) ? highlight.Order : 0;
    }

    internal static void Highlight(long sender, ZDOID id, UserInfo user, bool isPlayer = false)
    {
        if (Enabled.IsOn() && (isPlayer ? PlayersEnabled.IsOn() : MobHighlightEnabled.IsOn())
            && !id.IsNone() && Player.m_localPlayer)
        {
            Update();
            if (!Highlights.TryGetValue(sender, out var highlights))
                Highlights[sender] = highlights = [];
            float duration = isPlayer ? PlayerHighlightDuration.Value : MobHighlightDuration.Value;
            highlights[id] = (Time.unscaledTime + duration, ++_nextOrder, user, isPlayer);
            Trim();
        }
    }

    private static void Trim()
    {
        ActiveHighlights.Clear();
        bool mobsEnabled = MobHighlightEnabled.IsOn();
        bool playersEnabled = PlayersEnabled.IsOn();
        int limit = MaxHighlightedMobs.Value;
        foreach (var playerEntry in Highlights)
        {
            var highlights = playerEntry.Value;
            foreach (var entry in highlights)
                if (entry.Value.Expiration <= Time.unscaledTime
                    || !(entry.Value.IsPlayer ? playersEnabled : mobsEnabled)) Expired.Add(entry.Key);
            foreach (ZDOID id in Expired) highlights.Remove(id);
            Expired.Clear();

            while (highlights.Count > limit)
            {
                ZDOID oldest = ZDOID.None;
                long oldestOrder = long.MaxValue;
                foreach (var entry in highlights)
                {
                    if (entry.Value.Order >= oldestOrder) continue;
                    oldest = entry.Key;
                    oldestOrder = entry.Value.Order;
                }
                highlights.Remove(oldest);
            }
            foreach (var entry in highlights)
                if (!ActiveHighlights.TryGetValue(entry.Key, out var active) || entry.Value.Order > active.Order)
                    ActiveHighlights[entry.Key] = entry.Value;
            if (highlights.Count == 0) ExpiredPlayers.Add(playerEntry.Key);
        }
        foreach (long sender in ExpiredPlayers) Highlights.Remove(sender);
        ExpiredPlayers.Clear();
        _indexedFrame = Time.frameCount;
        _indexedLimit = limit;
        _indexedMobsEnabled = mobsEnabled;
        _indexedPlayersEnabled = playersEnabled;
    }
}