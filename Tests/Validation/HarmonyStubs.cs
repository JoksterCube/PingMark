using System;

namespace JoksterCube.PingMark.Validation.HarmonyLib;

[AttributeUsage(AttributeTargets.Class)]
public class HarmonyPatch : Attribute { public HarmonyPatch(Type type, string method) { } }