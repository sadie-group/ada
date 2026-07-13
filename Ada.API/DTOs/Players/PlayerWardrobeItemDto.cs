using Ada.Core.Enums.Game.Players;

namespace Ada.API.DTOs.Players;

public record PlayerWardrobeItemDto
{
    public int Id { get; set; }
    public int SlotId { get; set; }
    public string? FigureCode { get; set; }
    public PlayerAvatarGender Gender { get; set; }
}
