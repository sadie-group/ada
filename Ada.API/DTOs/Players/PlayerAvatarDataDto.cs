using Ada.Core.Enums.Game.Players;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.API.DTOs.Players;

public record PlayerAvatarDataDto
{
    public string? FigureCode { get; set; }
    public string? Motto { get; set; }
    public PlayerAvatarGender Gender { get; set; }
    public ChatBubble ChatBubbleId { get; set; }
}