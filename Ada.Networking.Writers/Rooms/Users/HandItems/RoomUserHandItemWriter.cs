using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.HandItems;

[PacketId(ServerPacketId.RoomUserHandItem)]
public class RoomUserHandItemWriter : AbstractPacketWriter
{
    public required long UserId { get; init; }
    public required int ItemId { get; init; }
}