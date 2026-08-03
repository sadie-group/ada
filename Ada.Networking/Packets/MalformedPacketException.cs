namespace Ada.Networking.Packets;

public sealed class MalformedPacketException(string message) : Exception(message);
