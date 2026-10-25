using System.Collections.Generic;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain.Networking;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

internal static class MobHighlight
{
    private static readonly HighlightRegistry<ZDOID, UserInfo> Registry = new();
    private static int _indexedFrame = -1;
    private static int _indexedLimit;
    private static bool _indexedMobsEnabled;
    private static bool _indexedPlayersEnabled;

    internal static void Update()
    {
        if (!Enabled.IsOn() || !Player.m_localPlayer || (!MobHighlightEnabled.IsOn() && !PlayersEnabled.IsOn()))
        {
            Clear();
            return;
        }

        if (_indexedFrame != Time.frameCount || _indexedLimit != MaxHighlightedMobs.Value
            || _indexedMobsEnabled != MobHighlightEnabled.IsOn() || _indexedPlayersEnabled != PlayersEnabled.IsOn())
            Trim();
    }

    internal static void Clear()
    {
        Registry.Clear();
        _indexedFrame = -1;
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
            Registry.Remove(id);
            return false;
        }
        return Registry.IsActive(id);
    }

    internal static void GetMarkedIds(List<ZDOID> ids)
    {
        Update();
        Registry.CopyActiveIds(ids);
    }

    internal static bool ShouldDrawOutline(Character character) =>
        IsMarked(character) && (character.IsPlayer()
            ? PingOutlineEnabled.IsOn() && (!HideOwnPlayerOutline.IsOn() || character != Player.m_localPlayer)
            : MobOutlineEnabled.IsOn());

    internal static string GetPingerName(Character character)
    {
        Update();
        return Registry.TryGetActive(character.GetZDOID(), out var highlight) ? highlight.User.GetDisplayName() : string.Empty;
    }

    internal static long GetPingerOrder(Character character)
    {
        Update();
        return Registry.TryGetActive(character.GetZDOID(), out var highlight) ? highlight.Order : 0;
    }

    // Unlimited highlights (Perception creature scans) do not count toward Max Highlighted Mobs, and last for
    // Perception's own duration instead of the highlight duration.
    internal static void Highlight(long sender, ZDOID id, UserInfo user, bool isPlayer = false, bool unlimited = false,
        float? duration = null)
    {
        if (!Enabled.IsOn() || !(isPlayer ? PlayersEnabled.IsOn() : MobHighlightEnabled.IsOn())
            || id.IsNone() || !Player.m_localPlayer) return;

        Update();
        float seconds = duration ?? (isPlayer ? PlayerHighlightDuration.Value : MobHighlightDuration.Value);
        Registry.Add(sender, id, user, isPlayer, Time.unscaledTime + seconds, unlimited);
        Trim();
    }

    private static void Trim()
    {
        _indexedLimit = MaxHighlightedMobs.Value;
        _indexedMobsEnabled = MobHighlightEnabled.IsOn();
        _indexedPlayersEnabled = PlayersEnabled.IsOn();
        _indexedFrame = Time.frameCount;
        Registry.Trim(Time.unscaledTime, _indexedLimit, _indexedMobsEnabled, _indexedPlayersEnabled);
    }
}
