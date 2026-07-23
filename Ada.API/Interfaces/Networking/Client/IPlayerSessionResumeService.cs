using Ada.API.Interfaces.Game.Players;

namespace Ada.API.Interfaces.Networking.Client;

public interface IPlayerSessionResumeService
{
    Task<bool> TryResumeAsync(IPlayerLogic existingPlayer, INetworkClient newClient);
}
