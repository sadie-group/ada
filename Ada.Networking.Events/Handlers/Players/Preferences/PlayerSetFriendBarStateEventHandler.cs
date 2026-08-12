using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Preferences;

[PacketId(EventHandlerId.PlayerSetFriendBarState)]
public class PlayerSetFriendBarStateEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IDefersPersistence
{
    public int State { get; init; }

    private const int _maxState = 3;

    public Task HandleAsync(INetworkClient client)
    {
        var settings = client.Player?.Player.GameSettings;

        if (settings == null)
        {
            return Task.CompletedTask;
        }

        settings.UiFlags = Math.Clamp(State, 0, _maxState);

        var playerId = client.Player!.Player.Id;
        var uiFlags = settings.UiFlags;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.Set<PlayerGameSettings>()
                .Where(x => x.PlayerId == playerId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.UiFlags, uiFlags));
        };

        return Task.CompletedTask;
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
