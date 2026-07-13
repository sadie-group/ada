namespace Ada.API.DTOs.Players;

public record PlayerRoomLikeDto
{
    public int Id { get; set; }
    public long PlayerId { get; set; }
    public int RoomId { get; set; }
}