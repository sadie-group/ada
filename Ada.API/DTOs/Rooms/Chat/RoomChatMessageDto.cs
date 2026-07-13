using Ada.API.DTOs.Players;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.API.DTOs.Rooms.Chat;

public record RoomChatMessageDto
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public long PlayerId { get; init; }
    public PlayerDto? Player { get; init; }
    public string? Message { get; init; }
    public ChatBubble ChatBubbleId { get; init; }
    public RoomUserEmotion EmotionId { get; init; }
    public RoomChatMessageType TypeId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}