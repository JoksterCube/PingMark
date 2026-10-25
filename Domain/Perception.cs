using System.Collections.Generic;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain.Enums;
using JoksterCube.PingMark.Domain.Models;
using JoksterCube.PingMark.Domain.Networking;
using JoksterCube.PingMark.Domain.PingTargets;
using UnityEngine;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain;

// Outlines listed objects and drops around the local player for Perception Duration, without sending a ping, and
// with Perceive Creatures also highlights nearby creatures. Perception Fatigue starts once Perception ends.
internal static class Perception
{
    private const string ActiveEffectName = "PingMark_Perception";
    private const string FatigueEffectName = "PingMark_PerceptionFatigue";
    // An eye for sensing, and a burnt-out lump of coal for the rest after it.
    private const string ActiveIconItem = "GreydwarfEye";
    private const string FatigueIconItem = "Coal";
    // Checking a few dozen references twice a second costs nothing noticeable.
    private const float EarlyEndCheckInterval = 0.5f;

    private static readonly List<(PingTarget Target, ZDOID Id, float Distance)> Found = [];
    private static readonly HashSet<GameObject> Seen = [];
    private static readonly List<ZDOID> SharedIds = [];
    private static readonly List<(Character Character, float Distance, bool Enemy)> Creatures = [];
    private static readonly List<ZDOID> CreatureIds = [];
    // What can run out before Perception does: drops, pickables, chests, deposits and enemies.
    private static readonly List<Object> Remaining = [];
    private static int _enemyCount;
    private static bool _active;
    private static float _activeUntil;
    private static float _nextEarlyEndCheck;
    private static float _pendingFatigue;
    private static float _readyAt;
    private static StatusEffect? _activeEffect;
    private static StatusEffect? _fatigueEffect;

    internal static void Clear()
    {
        _active = false;
        _readyAt = 0f;
        Remaining.Clear();
    }

    internal static void Update()
    {
        if (!_active) return;
        float now = Time.time;
        if (now >= _activeUntil) End(false);
        else if (PerceptionEarlyEnd.IsOn() && now >= _nextEarlyEndCheck)
        {
            _nextEarlyEndCheck = now + EarlyEndCheckInterval;
            if (Remaining.Count > 0 && AllGone()) End(true);
        }
    }

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
        if (_active)
        {
            PlayerMessages.Center($"Perception is active ({Mathf.CeilToInt(_activeUntil - now)}s)");
            return;
        }
        if (now < _readyAt)
        {
            PlayerMessages.Center($"Perception Fatigue ({Mathf.CeilToInt(_readyAt - now)}s)");
            return;
        }

        PerceptionEffect.Play(player);
        float duration = PerceptionDuration.Value;
        float until = now + duration;
        Remaining.Clear();
        Collect(player);
        // Every outlined renderer is drawn several times per frame, so a cluttered room is capped to the nearest.
        int count = Mathf.Min(Found.Count, PerceptionMaxTargets.Value);
        SharedIds.Clear();
        int drops = 0;
        for (int index = 0; index < count; index++)
        {
            if (Found[index].Target.Category == PingCategory.Drop) drops++;
            Outline(Found[index].Target, until);
            TrackRemaining(Found[index].Target);
            if (!Found[index].Id.IsNone()) SharedIds.Add(Found[index].Id);
        }
        CollectCreatures(player);
        foreach (ZDOID id in CreatureIds)
            MobHighlight.Highlight(ZNet.GetUID(), id, UserInfo.GetLocalUser(), unlimited: true, duration: duration);
        if (count == 0 && CreatureIds.Count == 0) PlayerMessages.TopLeft("You sense nothing of note nearby.");
        // Nearby PingMark players see the same objects, resolved against their own collections, and creatures.
        ZPingNetwork.SendPerception(SharedIds, CreatureIds);

        // Fatigue is decided by what was found, but only starts once Perception ends.
        _pendingFatigue = FatigueFor(count - drops, drops, CreatureIds.Count - _enemyCount, _enemyCount);
        _active = true;
        _activeUntil = until;
        _nextEarlyEndCheck = now + EarlyEndCheckInterval;
        _readyAt = until + _pendingFatigue;
        ApplyEffect(player, ref _activeEffect, ActiveEffectName, "Perception",
            "Your senses are heightened: nearby treasures and creatures stand out.", ActiveIconItem, duration);
    }

    private static void End(bool early)
    {
        _active = false;
        Remaining.Clear();
        _readyAt = Time.time + _pendingFatigue;
        Player player = Player.m_localPlayer;
        if (!player) return;
        if (early && _activeEffect) player.GetSEMan().RemoveStatusEffect(_activeEffect!.NameHash(), true);
        ApplyEffect(player, ref _fatigueEffect, FatigueEffectName, "Perception Fatigue",
            "Your senses need rest before you can use Perception again.", FatigueIconItem, _pendingFatigue);
    }

    // Only things that can run out count: a portal or crafting station nearby never ends Perception early.
    private static void TrackRemaining(PingTarget target)
    {
        foreach (GameObject outline in target.OutlineTargets)
        {
            if (!outline) continue;
            if (outline.TryGetComponent(out ItemDrop drop)) Remaining.Add(drop);
            // Already picked plants and empty chests have nothing left to take, so they would end Perception at once.
            else if (outline.TryGetComponent(out Pickable pickable)) { if (!pickable.GetPicked()) Remaining.Add(pickable); }
            else if (outline.TryGetComponent(out PickableItem pickableItem)) Remaining.Add(pickableItem);
            else if (outline.TryGetComponent(out Container container))
            {
                if (container.GetInventory() is { } inventory && inventory.NrOfItems() > 0) Remaining.Add(container);
            }
            else if (outline.GetComponent<IDestructible>() is Component destructible) Remaining.Add(destructible);
        }
    }

    // Destroyed objects compare equal to null; picked plants, emptied chests and dead enemies remain but are done.
    private static bool AllGone()
    {
        foreach (Object remaining in Remaining)
        {
            if (!remaining) continue;
            switch (remaining)
            {
                case Character character when !character.IsDead(): return false;
                case Pickable pickable when !pickable.GetPicked(): return false;
                case Container container when container.GetInventory() is { } inventory && inventory.NrOfItems() > 0: return false;
                case Character or Pickable or Container: continue;
                default: return false;
            }
        }
        return true;
    }

    internal static void ShowRemote(long sender, UserInfo user, List<ZDOID> ids, List<ZDOID> creatureIds)
    {
        if (!Enabled.IsOn() || !PerceptionEnabled.IsOn() || !ZNetScene.instance) return;
        float duration = PerceptionDuration.Value;
        foreach (Player other in Player.GetAllPlayers())
            if (other && other.GetOwner() == sender)
            {
                PerceptionEffect.Play(other);
                break;
            }
        float until = Time.time + duration;
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
                MobHighlight.Highlight(sender, id, user, unlimited: true, duration: duration);
        }
    }

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

    // Relative mode charges for what was sensed: a busy room or a pack of enemies tires the senses for longer.
    internal static float FatigueFor(int objects, int drops, int allies, int enemies)
    {
        float fatigue = PerceptionFatigueDuration.Value;
        if (PerceptionFatigueMode.Value != FatigueMode.Relative) return fatigue;
        fatigue += objects * PerceptionObjectCost.Value + drops * PerceptionDropCost.Value + allies * PerceptionAllyCost.Value + enemies * PerceptionEnemyCost.Value;
        return Mathf.Min(fatigue, Mathf.Max(PerceptionMaxFatigue.Value, PerceptionFatigueDuration.Value));
    }

    // Hostile and tamed creatures alike, nearest first; players are left to the Ping Shortcut.
    private static void CollectCreatures(Player player)
    {
        Creatures.Clear();
        CreatureIds.Clear();
        _enemyCount = 0;
        if (!PerceptionCreaturesEnabled.IsOn() || !MobHighlightEnabled.IsOn()) return;
        Vector3 center = player.GetCenterPoint();
        float range = PerceptionRange.Value;
        foreach (Character character in Character.GetAllCharacters())
        {
            // Merchants are outlined as listed objects instead.
            if (!character || character.IsPlayer() || character.IsDead() || PingTargetResolver.IsMerchant(character.gameObject)) continue;
            float distance = (character.GetCenterPoint() - center).sqrMagnitude;
            ZDOID id = character.GetZDOID();
            // Tamed creatures, neutral Dvergr and other non-hostiles count as allies.
            if (distance <= range * range && !id.IsNone()) Creatures.Add((character, distance, BaseAI.IsEnemy(player, character)));
        }
        Creatures.Sort((left, right) => left.Distance.CompareTo(right.Distance));
        int count = Mathf.Min(Creatures.Count, PerceptionMaxTargets.Value);
        for (int index = 0; index < count; index++)
        {
            CreatureIds.Add(Creatures[index].Character.GetZDOID());
            if (!Creatures[index].Enemy) continue;
            _enemyCount++;
            Remaining.Add(Creatures[index].Character);
        }
    }

    private static void ApplyEffect(Player player, ref StatusEffect? effect, string name, string displayName, string tooltip,
        string iconItem, float duration)
    {
        if (duration <= 0f) return;
        if (!effect)
        {
            effect = ScriptableObject.CreateInstance<StatusEffect>();
            effect.name = name;
            effect.m_name = displayName;
            effect.m_tooltip = tooltip;
            effect.hideFlags = HideFlags.HideAndDontSave;
        }
        // The HUD only lists effects with an icon; ObjectDB may not be ready on the first use after loading.
        if (!effect!.m_icon) effect.m_icon = FindIcon(iconItem);
        effect.m_ttl = duration;
        player.GetSEMan().AddStatusEffect(effect, true, 0, 0f, 0);
    }

    private static Sprite? FindIcon(string itemName)
    {
        if (!ObjectDB.instance) return null;
        GameObject? item = ObjectDB.instance.GetItemPrefab(itemName);
        ItemDrop? drop = item ? item!.GetComponent<ItemDrop>() : null;
        return drop ? drop!.m_itemData.GetIcon() : null;
    }
}
