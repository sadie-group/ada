namespace Ada.API.Interfaces.Networking.Client;

public interface IClientDisposalService
{
    Task HandleDisconnectAsync(INetworkClient client);
}
