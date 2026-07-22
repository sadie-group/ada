using Ada.Core.Enums.Game.WordFilter;

namespace Ada.API.DTOs.Server;

public class WordFilterResultDto
{
    public required string OriginalText { get; init; }
    public required string FilteredText { get; init; }
    public WordFilterAction? Action { get; init; }
    public List<string> MatchedPatterns { get; init; } = [];

    public bool WasModified => OriginalText != FilteredText;
    public bool IsBlocked => Action == WordFilterAction.Block;
    public bool IsShadowBlocked => Action == WordFilterAction.ShadowBlock;
    public bool HasMatch => MatchedPatterns.Count > 0;
}
