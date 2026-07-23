using Ada.Core.Enums.Game.WordFilter;

namespace Ada.Db.Models.Server;

public class WordFilterEntry
{
    public int Id { get; set; }
    public required string Pattern { get; set; }
    public WordFilterMatchType MatchTypeId { get; set; }
    public WordFilterAction ActionId { get; set; }
    public WordFilterContext Contexts { get; set; } = WordFilterContext.All;
    public string? Replacement { get; set; }
    public bool NormalizeText { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
