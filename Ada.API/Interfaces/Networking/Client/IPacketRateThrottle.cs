namespace Ada.API.Interfaces.Networking.Client;

public interface IPacketRateThrottle
{
    bool TryConsume(Guid clientGuid);

    void Forget(Guid clientGuid);
}
