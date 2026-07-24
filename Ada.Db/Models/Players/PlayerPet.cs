using System.ComponentModel;

namespace Ada.Db.Models.Players;

public class PlayerPet
{
    public int Id { get; init; }
    public required long PlayerId { get; set; }
    public required int? RoomId { get; set; }
    public required string Name { get; set; }
    public required int Type { get; init; }
    public required int Race { get; init; }
    public required string Color { get; init; }
    [DefaultValue(1)] public int Level { get; set; }
    [DefaultValue(0)] public int Experience { get; set; }
    [DefaultValue(100)] public int Energy { get; set; }
    [DefaultValue(100)] public int Happiness { get; set; }
    [DefaultValue(0)] public int Respect { get; set; }
    [DefaultValue(false)] public bool HasSaddle { get; set; }
    [DefaultValue(false)] public bool AnyoneCanRide { get; set; }
    [DefaultValue(-1)] public int HairStyle { get; set; }
    [DefaultValue(-1)] public int HairColor { get; set; }
    [DefaultValue(false)] public bool PubliclyBreedable { get; set; }
    [DefaultValue(0)] public int GrowthStage { get; set; }
    [DefaultValue(0)] public int Rarity { get; set; }
    [DefaultValue(false)] public bool IsDead { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public double Z { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
}
