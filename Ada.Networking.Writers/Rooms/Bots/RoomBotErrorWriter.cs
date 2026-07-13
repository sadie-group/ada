using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Bots;

[PacketId(ServerPacketId.RoomBotError)]
public class RoomBotErrorWriter : AbstractPacketWriter
{
    public required int ErrorCode { get; init; }
    
}