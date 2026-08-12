namespace Ada.API.Interfaces.Networking.Client;

public interface IWalkRequestThrottle
{
    bool TryConsume(long playerId);

    void Forget(long playerId);
}
