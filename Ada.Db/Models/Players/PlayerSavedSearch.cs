using System.ComponentModel.DataAnnotations.Schema;
using Ada.Core.Shared.Attributes;

namespace Ada.Db.Models.Players;

public class PlayerSavedSearch
{
    [PacketData] public int Id { get; init; }
    [PacketData] public string? Search { get; init; }
    [PacketData] public string? Filter { get; init; }
    [NotMapped] [PacketData] public string Localization => "";
    public long PlayerId { get; init; }
}