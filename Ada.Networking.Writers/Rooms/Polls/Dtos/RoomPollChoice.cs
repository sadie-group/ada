using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Polls.Dtos;

public class RoomPollChoice
{
    [PacketData] public required string Value { get; init; }
    [PacketData] public required string Text { get; init; }
    [PacketData] public required int Type { get; init; }
}