using Ada.API.DTOs.Players;

namespace Ada.API.Interfaces.Game.Players;

public interface IPlayerLoaderService
{
    Task<PlayerSsoTokenDto?> GetTokenAsync(string token);
}