using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Preferences;

[PacketId(EventHandlerId.PlayerSetMessengerInviteStatus)]
public class PlayerSetMessengerInviteStatusEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IDefersPersistence
{
    public bool DisableInvites { get; init; }

    public Task HandleAsync(INetworkClient client)
    {
        var settings = client.Player?.Player.GameSettings;

        if (settings == null)
        {
            return Task.CompletedTask;
        }

        settings.BlockRoomInvites = DisableInvites;

        var playerId = client.Player!.Player.Id;
        var blockRoomInvites = settings.BlockRoomInvites;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.Set<PlayerGameSettings>()
                .Where(x => x.PlayerId == playerId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.BlockRoomInvites, blockRoomInvites));
        };

        return Task.CompletedTask;
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
