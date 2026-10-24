using static MessageHud;

namespace JoksterCube.PingMark.Common;

internal static class PlayerMessages
{
    internal static void TopLeft(string message)
    {
        if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageType.TopLeft, message);
    }

    internal static void Center(string message)
    {
        if (Player.m_localPlayer) Player.m_localPlayer.Message(MessageType.Center, message);
    }
}
