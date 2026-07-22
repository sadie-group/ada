namespace Ada.API.Interfaces.Game.Rooms.Services;

public interface IRoomFloodProtectionService
{
    bool IsMuted(long playerId, out int remainingSeconds);
    int? RegisterMessage(long playerId, int chatProtection, bool bypass);
    void Clear(long playerId);
}
