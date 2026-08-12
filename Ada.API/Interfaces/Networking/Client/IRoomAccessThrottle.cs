namespace Ada.API.Interfaces.Networking.Client;

public interface IRoomAccessThrottle
{
    bool TryConsume(long playerId, long roomId);
}
