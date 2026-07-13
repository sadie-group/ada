using System.ComponentModel.DataAnnotations;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.Db.Models.Players;

public class PlayerAvatarData
{
    public int Id { get; init; }
    public long PlayerId { get; init; }

    public Player? Player { get; init; }

    [MaxLength(200)]
    public required string FigureCode { get; set; }

    [MaxLength(50)]
    public string? Motto { get; set; }

    public PlayerAvatarGender Gender { get; set; }
    public ChatBubble ChatBubbleId { get; set; }
}