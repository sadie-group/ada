using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;

namespace Ada.Networking.Client;

public class PlayerSessionResumeService : IPlayerSessionResumeService
{
    public Task<bool> TryResumeAsync(IPlayerLogic existingPlayer, INetworkClient newClient)
    {
        return Task.FromResult(false);
    }
}
