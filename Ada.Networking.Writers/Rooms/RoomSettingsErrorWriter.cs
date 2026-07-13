using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomSettingsError)]
public class RoomSettingsErrorWriter : AbstractPacketWriter
{
    public required int RoomId { get; init; }
    public required int ErrorCode { get; init; }
    public required string Message { get; init; }
}