using Ada.API.Interfaces.Game.Players;

namespace Ada.API.Interfaces.Game.Guides;

public interface IGuideSessionService
{
    int GuidesOnDuty { get; }
    int GuardiansOnDuty { get; }
    int AverageWaitSeconds { get; }

    bool SetOnDuty(long playerId, bool onDuty, bool asGuardian);
    bool IsOnDuty(long playerId);

    IGuideSession? GetSessionForPlayer(long playerId);
    IGuideSession? GetPendingSessionForHelper(long helperId);

    IGuideSession CreateSession(long requesterId, int requestType, string helpRequest);
    long? TryAssignHelper(IGuideSession session, IEnumerable<IPlayerLogic> candidates);
    bool Accept(IGuideSession session, long helperId);
    void Decline(IGuideSession session);
    bool End(IGuideSession session);

    void Forget(long playerId);
}
