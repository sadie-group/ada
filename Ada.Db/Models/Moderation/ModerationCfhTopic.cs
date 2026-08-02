namespace Ada.Db.Models.Moderation;

public class ModerationCfhTopic
{
    public int Id { get; init; }
    public required string CategoryName { get; init; }
    public required string Name { get; init; }
    public int Order { get; init; }
}
