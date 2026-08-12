namespace Ada.Db.Models.Rooms;

public class RoomJukeboxTrack
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public int PlayerFurnitureItemId { get; init; }
    public int SoundTrackId { get; init; }
    public int OrderIndex { get; set; }
    public required DateTime CreatedAt { get; init; }
}
