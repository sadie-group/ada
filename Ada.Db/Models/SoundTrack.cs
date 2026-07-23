namespace Ada.Db.Models;

public class SoundTrack
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public required string Author { get; set; }
    public required string Code { get; set; }
    public required string Data { get; set; }
    public int Length { get; set; }
}
