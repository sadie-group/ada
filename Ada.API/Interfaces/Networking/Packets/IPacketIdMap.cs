using System.Diagnostics.CodeAnalysis;

namespace Ada.API.Interfaces.Networking.Packets;

public interface IPacketIdMap
{
    bool TryGetHandlerType(short packetId, [NotNullWhen(true)] out Type? handlerType);
    bool TryGetOutgoingId(Type writerType, out short packetId);
}
