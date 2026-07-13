using Ada.Core.Enums.Game.Players;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.API.DTOs.Players;

public record PlayerBotDto
{
    public int Id { get; init; }
    public long PlayerId { get; init; }
    public int? RoomId { get; set; }
    public string? Username { get; init; }
    public string? FigureCode { get; init; }
    public string? Motto { get; init; }
    public PlayerAvatarGender Gender { get; init; }
    public ChatBubble ChatBubbleId { get; init; }
    public DateTime CreatedAt { get; init; }
}
