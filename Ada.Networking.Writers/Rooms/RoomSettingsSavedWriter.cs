using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomSettingsSaved)]
public class RoomSettingsSavedWriter : AbstractPacketWriter
{
    public required long RoomId { get; init; }
}