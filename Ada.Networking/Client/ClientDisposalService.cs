using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Networking.Client;

namespace Ada.Networking.Client;

public class ClientDisposalService(
    INetworkClientRepository clientRepository,
    IWalkRequestThrottle walkThrottle,
    IGuideSessionService guideSessionService) : IClientDisposalService
{
    public async Task HandleDisconnectAsync(INetworkClient client)
    {
        var playerId = client.Player?.Player.Id;

        if (playerId.HasValue)
        {
            walkThrottle.Forget(playerId.Value);
            guideSessionService.Forget(playerId.Value);
        }

        await clientRepository.TryRemoveAsync(client.Guid);
        await client.DisposeAsync();
    }
}
