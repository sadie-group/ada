using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Moderation;

public class PatternMatchData
{
    [PacketData] public required string Pattern { get; init; }
    [PacketData] public required int StartIndex { get; init; }
    [PacketData] public required int EndIndex { get; init; }
}