using System;
using HarmonyLib;
using JoksterCube.PingDistance.Common;
using UnityEngine;
using JoksterCube.PingDistance.Settings;
using static JoksterCube.PingDistance.Settings.PluginConfig;

namespace JoksterCube.PingDistance.Domain;

internal static class MobTargeting
{
    private static readonly Func<Character, bool> TakeInput =
        AccessTools.MethodDelegate<Func<Character, bool>>(AccessTools.Method(typeof(Character), nameof(Character.TakeInput)));
    private static Vector3? _pendingGroundPing;

    internal static bool TryConsumeGroundPing(Vector3 position)
    {
        if (Enabled.Value != Toggle.On) return false;

        if (_pendingGroundPing is not { } target || (target - position).sqrMagnitude > 0.01f) return false;
        _pendingGroundPing = null;
        return true;
    }

    internal static void TryHighlight()
    {
        if (Enabled.Value != Toggle.On) return;

        Player player = Player.m_localPlayer;
        if (!player || !TakeInput(player) || !GameCamera.instance) return;

        Transform cameraTransform = GameCamera.instance.transform;
        Ray ray = new(cameraTransform.position, cameraTransform.forward);
        float maxDistance = CameraPingMaxDistance.Value;
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, Constants.LayerMasks.MobHighlight, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        Vector3 position = ray.GetPoint(maxDistance);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(player.transform)) continue;
            Character? character = hit.collider.GetComponentInParent<Character>();
            if (character && !character.IsPlayer() && !character.IsDead())
            {
                MobHighlight.Mark(character);
                return;
            }
            position = hit.point;
            break;
        }

        if (ZoneSystem.instance
            && new Plane(Vector3.up, new Vector3(0f, ZoneSystem.instance.m_waterLevel, 0f)).Raycast(ray, out float waterDistance)
            && waterDistance < Vector3.Distance(ray.origin, position))
            position = ray.GetPoint(waterDistance);

        if (ZRoutedRpc.instance == null) return;
        _pendingGroundPing = position;
        ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", position, (int)Talker.Type.Ping, UserInfo.GetLocalUser(), "");
    }
}