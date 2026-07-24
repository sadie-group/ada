namespace Ada.API.DTOs.Players;

public record PlayerPetDto
{
    public int Id { get; init; }
    public long PlayerId { get; set; }
    public int? RoomId { get; set; }
    public string? Name { get; set; }
    public int Type { get; init; }
    public int Race { get; set; }
    public string? Color { get; init; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int Energy { get; set; }
    public int Happiness { get; set; }
    public int Respect { get; set; }
    public bool HasSaddle { get; set; }
    public bool AnyoneCanRide { get; set; }
    public int HairStyle { get; set; } = -1;
    public int HairColor { get; set; } = -1;
    public bool PubliclyBreedable { get; set; }
    public int GrowthStage { get; set; }
    public int Rarity { get; set; }
    public bool IsDead { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public double Z { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public string OwnerName { get; set; } = "";
}
