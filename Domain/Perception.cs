using System.Collections.Generic;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain.Enums;
using JoksterCube.PingMark.Domain.Models;
using JoksterCube.PingMark.Domain.Networking;
using JoksterCube.PingMark.Domain.PingTargets;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

// Outlines listed objects and drops around the local player for as long as a ping lasts, without sending a
// ping, and with Perceive Creatures also highlights nearby creatures. Perception Fatigue limits how often it can be used.
internal static class Perception
{
    private const string FatigueEffectName = "PingMark_PerceptionFatigue";
    // Wishbone already means "senses hidden things" to players, so its icon marks the fatigue.
    private const string FatigueIconItem = "Wishbone";
    private const float FallbackDuration = 5f;

    private static readonly List<(PingTarget Target, ZDOID Id, float Distance)> Found = [];
    private static readonly HashSet<GameObject> Seen = [];
    private static readonly List<ZDOID> SharedIds = [];
    private static readonly List<(ZDOID Id, float Distance)> Creatures = [];
    private static readonly List<ZDOID> CreatureIds = [];
    private static float _readyAt;
    private static StatusEffect? _fatigueEffect;

    internal static void Clear() => _readyAt = 0f;

    internal static void TryActivate()
    {
        Player player = Player.m_localPlayer;
        if (!player || !Enabled.IsOn()) return;
        if (!PerceptionEnabled.IsOn())
        {
            PlayerMessages.Center("Perception is disabled.");
            return;
        }

        float now = Time.time;
        if (now < _readyAt)
        {
            PlayerMessages.Center($"Perception Fatigue ({Mathf.CeilToInt(_readyAt - now)}s)");
            return;
        }

        float until = OutlineUntil();
        Collect(player);
        // Every outlined renderer is drawn several times per frame, so a cluttered room is capped to the nearest.
        int count = Mathf.Min(Found.Count, PerceptionMaxTargets.Value);
        SharedIds.Clear();
        for (int index = 0; index < count; index++)
        {
            Outline(Found[index].Target, until);
            if (!Found[index].Id.IsNone()) SharedIds.Add(Found[index].Id);
        }
        CollectCreatures(player);
        foreach (ZDOID id in CreatureIds) MobHighlight.Highlight(ZNet.GetUID(), id, UserInfo.GetLocalUser(), unlimited: true);
        if (count == 0 && CreatureIds.Count == 0) PlayerMessages.TopLeft("You sense nothing of note nearby.");
        // Nearby PingMark players see the same objects, resolved against their own collections, and creatures.
        ZPingNetwork.SendPerception(SharedIds, CreatureIds);

        float fatigue = PerceptionFatigueDuration.Value;
        _readyAt = now + fatigue;
        ApplyFatigue(player, fatigue);
    }

    internal static void ShowRemote(long sender, UserInfo user, List<ZDOID> ids, List<ZDOID> creatureIds)
    {
        if (!Enabled.IsOn() || !PerceptionEnabled.IsOn() || !ZNetScene.instance) return;
        float until = OutlineUntil();
        foreach (ZDOID id in ids)
        {
            GameObject instance = ZNetScene.instance.FindInstance(id);
            if (!instance) continue;
            if (PingTargetResolver.ResolveInstance(id, instance.transform.position) is { } target && IsPerceivable(target))
                Outline(target, until);
        }
        if (!PerceptionCreaturesEnabled.IsOn()) return;
        foreach (ZDOID id in creatureIds)
        {
            GameObject instance = ZNetScene.instance.FindInstance(id);
            if (instance && instance.TryGetComponent(out Character character) && !character.IsPlayer())
                MobHighlight.Highlight(sender, id, user, unlimited: true);
        }
    }

    private static float OutlineUntil() => Time.time + (Chat.instance ? Chat.instance.m_worldTextTTL : FallbackDuration);

    private static void Outline(PingTarget target, float until)
    {
        foreach (GameObject outline in target.OutlineTargets)
            if (outline) OutlineManager.AddTimedOutline(outline, target.Color, until);
    }

    // Same rules as a ping, so exactly what a ping would outline: enabled sections and drops.
    private static bool IsPerceivable(PingTarget target) => target.OutlineTargets.Length > 0
        && target.Category is PingCategory.Drop or PingCategory.PrefabSection;

    private static void Collect(Player player)
    {
        Found.Clear();
        Seen.Clear();
        Vector3 center = player.GetCenterPoint();
        foreach (Collider collider in Physics.OverlapSphere(center, PerceptionRange.Value,
                     Settings.Constants.LayerMasks.MobHighlight, QueryTriggerInteraction.Collide))
        {
            if (!collider || !PingTargetResolver.IsPingCollider(collider)) continue;
            if (collider.transform.IsChildOf(player.transform)) continue;
            if (!Seen.Add(PingTargetResolver.GetPrefabRoot(collider))) continue;

            Vector3 point = collider.bounds.center;
            if (PingTargetResolver.Resolve(collider, point) is not { } target || !IsPerceivable(target)) continue;
            Found.Add((target, PingTargetResolver.GetNetworkId(collider), (point - center).sqrMagnitude));
        }
        Found.Sort((left, right) => left.Distance.CompareTo(right.Distance));
    }

    // Hostile and tamed creatures alike, nearest first; players are left to the Ping Shortcut.
    private static void CollectCreatures(Player player)
    {
        Creatures.Clear();
        CreatureIds.Clear();
        if (!PerceptionCreaturesEnabled.IsOn() || !MobHighlightEnabled.IsOn()) return;
        Vector3 center = player.GetCenterPoint();
        float range = PerceptionRange.Value;
        foreach (Character character in Character.GetAllCharacters())
        {
            if (!character || character.IsPlayer() || character.IsDead()) continue;
            float distance = (character.GetCenterPoint() - center).sqrMagnitude;
            ZDOID id = character.GetZDOID();
            if (distance <= range * range && !id.IsNone()) Creatures.Add((id, distance));
        }
        Creatures.Sort((left, right) => left.Distance.CompareTo(right.Distance));
        int count = Mathf.Min(Creatures.Count, PerceptionMaxTargets.Value);
        for (int index = 0; index < count; index++) CreatureIds.Add(Creatures[index].Id);
    }

    private static void ApplyFatigue(Player player, float duration)
    {
        if (duration <= 0f) return;
        if (!_fatigueEffect)
        {
            _fatigueEffect = ScriptableObject.CreateInstance<StatusEffect>();
            _fatigueEffect.name = FatigueEffectName;
            _fatigueEffect.m_name = "Perception Fatigue";
            _fatigueEffect.m_tooltip = "Your senses need rest before you can use Perception again.";
            _fatigueEffect.hideFlags = HideFlags.HideAndDontSave;
        }
        // The HUD only lists effects with an icon; ObjectDB may not be ready on the first use after loading.
        if (!_fatigueEffect!.m_icon) _fatigueEffect.m_icon = FindIcon();
        _fatigueEffect.m_ttl = duration;
        player.GetSEMan().AddStatusEffect(_fatigueEffect, true, 0, 0f, 0);
    }

    private static Sprite? FindIcon()
    {
        if (!ObjectDB.instance) return null;
        GameObject? item = ObjectDB.instance.GetItemPrefab(FatigueIconItem);
        ItemDrop? drop = item ? item!.GetComponent<ItemDrop>() : null;
        return drop ? drop!.m_itemData.GetIcon() : null;
    }
}
