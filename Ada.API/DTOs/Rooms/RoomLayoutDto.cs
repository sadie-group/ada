namespace Ada.API.DTOs.Rooms;

public record RoomLayoutDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Heightmap { get; set; }
    public int DoorX { get; set; }
    public int DoorY { get; set; }
    public int DoorDirection { get; set; }
    public bool RequiresClubMembership { get; set; }
    public string? ExtraData { get; set; }
}