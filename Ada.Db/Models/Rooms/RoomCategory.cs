using Ada.Core.Shared.Attributes;

namespace Ada.Db.Models.Rooms;

public class RoomCategory
{
    [PacketData] public int Id { get; init; }
    [PacketData] public string? Caption { get; init; }
    [PacketData] public bool IsVisible { get; init; }
}