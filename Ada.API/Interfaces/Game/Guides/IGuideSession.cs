namespace Ada.API.Interfaces.Game.Guides;

public interface IGuideSession
{
    long RequesterId { get; }
    long? HelperId { get; }
    string HelpRequest { get; }
    int RequestType { get; }
    GuideSessionState State { get; }
    DateTimeOffset CreatedAt { get; }
}
