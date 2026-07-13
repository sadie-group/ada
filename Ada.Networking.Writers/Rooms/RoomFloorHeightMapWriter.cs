using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms;

[PacketId(ServerPacketId.RoomHeightMap)]
public class RoomFloorHeightMapWriter : AbstractPacketWriter
{
    public required bool Scale { get; init; }
    public required int WallHeight { get; init; }
    public required string RelativeHeightmap { get; init; }
}