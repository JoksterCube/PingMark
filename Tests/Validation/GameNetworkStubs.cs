using System;
using System.Collections.Generic;
using JoksterCube.PingMark.Validation.UnityEngine;

namespace JoksterCube.PingMark;

public class Player
{
    public static Player m_localPlayer = new();
    public Transform transform = new();
    public static implicit operator bool(Player value) => value != null;
}
public class UserInfo
{
    public static UserInfo GetLocalUser() => new UserInfo();
    public void Deserialize(ref ZPackage package) { package.ReadString(); package.ReadString(); }
}
public class Talker { public enum Type { Ping = 3 } }
public struct ZDOID { public bool IsNone() => true; }
public class ZPackage
{
    public readonly List<object> Values = [];
    private int position;
    public void SetPos(int value) { position = value; }
    public Vector3 ReadVector3() => (Vector3)Values[position++];
    public int ReadInt() => (int)Values[position++];
    public string ReadString() => (string)Values[position++];
}
public class ZRpc
{
    public int Sent;
    public void Invoke(string name, ZPackage package) { Sent++; }
}
public class ZNetPeer
{
    public long m_uid;
    public Vector3 m_refPos;
    public ZRpc m_rpc = new ZRpc();
    public bool Ready = true;
    public bool IsReady() => Ready;
}
public class ZNet
{
    public static ZNet instance = new ZNet();
    public readonly List<ZNetPeer> Peers = new List<ZNetPeer>();
    public bool Server = true;
    public bool IsServer() => Server;
    public static long GetUID() => 1;
    public ZNetPeer GetPeer(long id) => Peers.Find(peer => peer.m_uid == id);
    public List<ZNetPeer> GetConnectedPeers() => Peers;
    public static implicit operator bool(ZNet value) => value != null;
}
public static class StringExtensions { public static int GetStableHashCode(this string value) => value.GetHashCode(); }
public class ZRoutedRpc
{
    public const long Everybody = 0;
    public static ZRoutedRpc instance = new ZRoutedRpc();
    public readonly List<string> Sent = new List<string>();
    public class RoutedRPCData
    {
        public long m_targetPeerID, m_senderPeerID;
        public ZDOID m_targetZDO;
        public int m_methodHash;
        public ZPackage m_parameters = new ZPackage();
        public void Serialize(ZPackage package) { package.Values.AddRange(m_parameters.Values); }
    }
    public void Register<T1, T2, T3, T4>(string name, Action<long, T1, T2, T3, T4> handler) { }
    public void Register<T1, T2, T3, T4, T5>(string name, Action<long, T1, T2, T3, T4, T5> handler) { }
    public void InvokeRoutedRPC(long target, string name, params object[] args) { Sent.Add(name); }
}
public class Chat
{
    public static Chat instance = new Chat();
    public void OnNewChatMessage(object source, long sender, Vector3 position, Talker.Type type, UserInfo user, string text) { }
    public static implicit operator bool(Chat value) => value != null;
}