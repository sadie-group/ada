namespace Ada.API.DTOs.Moderation;

public record ModToolChatRoomDto
{
    public int RoomId { get; init; }
    public required string RoomName { get; init; }
    public required IReadOnlyList<ModToolChatLineDto> Lines { get; init; }
}
