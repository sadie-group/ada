namespace Ada.API.Interfaces.Game.Players;

public interface IPlayerPresenceStore
{
    Task SetOfflineAsync(long playerId);
}
