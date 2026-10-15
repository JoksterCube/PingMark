using System.Collections.Generic;

namespace JoksterCube.PingMark.Domain;

// Who highlighted what, without any Unity or game state: time and settings are passed in, so the rules
// (per-sender limit, expiry, newest highlight wins) can be unit tested.
internal sealed class HighlightRegistry<TKey, TUser> where TKey : notnull
{
    internal readonly struct Entry(float expiration, long order, TUser user, bool isPlayer)
    {
        internal float Expiration { get; } = expiration;
        internal long Order { get; } = order;
        internal TUser User { get; } = user;
        internal bool IsPlayer { get; } = isPlayer;
    }

    private readonly Dictionary<long, Dictionary<TKey, Entry>> _bySender = [];
    private readonly Dictionary<TKey, Entry> _active = [];
    private readonly List<TKey> _expired = [];
    private readonly List<long> _emptySenders = [];
    private long _nextOrder;

    internal int ActiveCount => _active.Count;

    internal void Add(long sender, TKey id, TUser user, bool isPlayer, float expiration)
    {
        if (!_bySender.TryGetValue(sender, out var highlights))
            _bySender[sender] = highlights = [];
        highlights[id] = new Entry(expiration, ++_nextOrder, user, isPlayer);
    }

    internal void Remove(TKey id)
    {
        foreach (var highlights in _bySender.Values) highlights.Remove(id);
        _active.Remove(id);
    }

    internal void Clear()
    {
        _bySender.Clear();
        _active.Clear();
    }

    internal bool IsActive(TKey id) => _active.ContainsKey(id);

    internal bool TryGetActive(TKey id, out Entry entry) => _active.TryGetValue(id, out entry);

    internal void CopyActiveIds(List<TKey> ids)
    {
        ids.Clear();
        ids.AddRange(_active.Keys);
    }

    // Drops expired or disabled highlights, keeps at most `limitPerSender` newest per sender and rebuilds
    // the active set, where the newest highlight of an object decides who is shown as the pinger.
    internal void Trim(float now, int limitPerSender, bool mobsEnabled, bool playersEnabled)
    {
        _active.Clear();
        foreach (var senderEntry in _bySender)
        {
            var highlights = senderEntry.Value;
            foreach (var entry in highlights)
                if (entry.Value.Expiration <= now || !(entry.Value.IsPlayer ? playersEnabled : mobsEnabled))
                    _expired.Add(entry.Key);
            foreach (TKey id in _expired) highlights.Remove(id);
            _expired.Clear();

            while (highlights.Count > limitPerSender) highlights.Remove(Oldest(highlights));

            foreach (var entry in highlights)
                if (!_active.TryGetValue(entry.Key, out var active) || entry.Value.Order > active.Order)
                    _active[entry.Key] = entry.Value;
            if (highlights.Count == 0) _emptySenders.Add(senderEntry.Key);
        }
        foreach (long sender in _emptySenders) _bySender.Remove(sender);
        _emptySenders.Clear();
    }

    private static TKey Oldest(Dictionary<TKey, Entry> highlights)
    {
        TKey oldest = default!;
        long oldestOrder = long.MaxValue;
        foreach (var entry in highlights)
        {
            if (entry.Value.Order >= oldestOrder) continue;
            oldest = entry.Key;
            oldestOrder = entry.Value.Order;
        }
        return oldest;
    }
}
