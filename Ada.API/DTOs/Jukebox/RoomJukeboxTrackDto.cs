namespace Ada.API.DTOs.Jukebox;

public record RoomJukeboxTrackDto
{
    public int Id { get; init; }
    public int PlayerFurnitureItemId { get; init; }
    public int SoundTrackId { get; init; }
    public int OrderIndex { get; init; }
}
