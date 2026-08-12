using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.StickyNotes;

[PacketId(ServerPacketId.RoomStickyNote)]
public class RoomStickyNoteWriter : AbstractPacketWriter
{
    public required string ItemId { get; init; }
    public required string Contents { get; init; }
}
