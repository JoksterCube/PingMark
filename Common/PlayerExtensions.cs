using static MessageHud;

namespace JoksterCube.PingMark.Common;

internal static class PlayerExtensions
{
    internal static void Message(MessageType messageType, string message) =>
        Player.m_localPlayer.Message(messageType, message);

    internal static void FormatedMessage(MessageType messageType, string message, params object[] args) =>
        Message(messageType, string.Format(message, args));

    internal static void FormatedCenterMessage(string message, params object[] args) =>
        FormatedMessage(MessageType.Center, message, args);

    internal static void FormatedTopLeftMessage(string message, params object[] args) =>
        FormatedMessage(MessageType.TopLeft, message, args);
}