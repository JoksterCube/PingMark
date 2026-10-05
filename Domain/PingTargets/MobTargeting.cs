using System;
using HarmonyLib;
using JoksterCube.PingMark.Common;
using JoksterCube.PingMark.Domain.Networking;
using JoksterCube.PingMark.Domain.Models;
using UnityEngine;
using JoksterCube.PingMark.Settings;
using static JoksterCube.PingMark.Settings.PluginConfig;

namespace JoksterCube.PingMark.Domain.PingTargets;

internal static class MobTargeting
{
    private static readonly Func<Character, bool> TakeInput =
        AccessTools.MethodDelegate<Func<Character, bool>>(AccessTools.Method(typeof(Character), nameof(Character.TakeInput)));
    private static Vector3? _pendingGroundPing;
    private static string? _pendingGroundPingName;
    private static PingTarget? _pendingGroundPingTarget;
    private static float? _lastPingTime;

    internal static void RegisterPendingGroundPing(Vector3 position, string? targetName = null, PingTarget? target = null)
    {
        _pendingGroundPing = position;
        _pendingGroundPingName = targetName;
        _pendingGroundPingTarget = target;
    }

    internal static bool TryConsumeGroundPing(Vector3 position, out string? targetName, out PingTarget? target)
    {
        targetName = null;
        target = null;
        if (!Enabled.IsOn()) return false;

        if (_pendingGroundPing is not { } pending || (pending - position).sqrMagnitude > 0.01f) return false;
        _pendingGroundPing = null;
        targetName = _pendingGroundPingName;
        _pendingGroundPingName = null;
        target = _pendingGroundPingTarget;
        _pendingGroundPingTarget = null;
        return true;
    }

    internal static void TryHighlight()
    {
        if (!Enabled.IsOn()) return;

        Player player = Player.m_localPlayer;
        if (!player || !TakeInput(player) || !GameCamera.instance) return;

        Transform cameraTransform = GameCamera.instance.transform;
        Ray ray = new(cameraTransform.position, cameraTransform.forward);
        float maxDistance = CameraPingMaxDistance.Value;
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, Constants.LayerMasks.MobHighlight, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        Vector3 position = ray.GetPoint(maxDistance);
        RaycastHit? targetHit = null;

        foreach (RaycastHit hit in hits)
        {
            if (!PingTargetResolver.IsPingCollider(hit.collider)) continue;
            if (hit.collider.transform.IsChildOf(player.transform)) continue;
            if (PingTargetResolver.GetGravestone(hit.collider))
            {
                if (!GravestonesEnabled.IsOn()) continue;
                targetHit = hit;
                position = hit.point;
                break;
            }

            Character? character = hit.collider.GetComponentInParent<Character>();
            if (character && character.IsPlayer())
            {
                if (!PlayersEnabled.IsOn()) continue;
                MobHighlight.Mark(character);
                return;
            }
            if (MobHighlightEnabled.IsOn() && character && !character.IsPlayer() && !character.IsDead())
            {
                MobHighlight.Mark(character);
                return;
            }
            targetHit = hit;
            position = hit.point;
            break;
        }

        if (ZoneSystem.instance
            && new Plane(Vector3.up, new Vector3(0f, ZoneSystem.instance.m_waterLevel, 0f)).Raycast(ray, out float waterDistance)
            && waterDistance < Vector3.Distance(ray.origin, position)
            && ZoneSystem.instance.m_waterLevel - position.y > MaxUnderwaterDepth.Value)
        {
            position = ray.GetPoint(waterDistance);
            targetHit = null;
        }

        PingTarget? target = null;
        if (targetHit.HasValue)
        {
            if (DebugPrefabId.IsOn() && Player.m_localPlayer)
            {
                string debugNames = PingTargetResolver.GetDebugNames(targetHit.Value);
                if (!string.IsNullOrWhiteSpace(debugNames)) PlayerExtensions.FormatedTopLeftMessage("{0}", debugNames);
            }

            target = PingTargetResolver.Resolve(targetHit.Value.collider, position);
        }

        if (ZRoutedRpc.instance == null) return;
        float now = Time.realtimeSinceStartup;
        if (_lastPingTime.HasValue && now - _lastPingTime.Value < PingCooldown.Value) return;
        string? targetName = target.HasValue && target.Value.Name.Length > 0 ? target.Value.Name : null;
        RegisterPendingGroundPing(position, targetName, target);
        ZPingNetwork.SendLocation(position, targetName ?? "");
        _lastPingTime = now;
    }
}