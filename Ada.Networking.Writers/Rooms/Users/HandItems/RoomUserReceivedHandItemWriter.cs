using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users.HandItems;

[PacketId(ServerPacketId.RoomUserReceivedHandItem)]
public class RoomUserReceivedHandItemWriter : AbstractPacketWriter
{
    public required long FromId { get; set; }
    public required int HandItemId { get; set; }
}