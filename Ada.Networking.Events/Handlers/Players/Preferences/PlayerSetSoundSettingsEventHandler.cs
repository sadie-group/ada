using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Preferences;

[PacketId(EventHandlerId.PlayerSetSoundSettings)]
public class PlayerSetSoundSettingsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IDefersPersistence
{
    public int SystemVolume { get; init; }
    public int FurnitureVolume { get; init; }
    public int TraxVolume { get; init; }

    private const int _maxVolume = 100;

    public Task HandleAsync(INetworkClient client)
    {
        var settings = client.Player?.Player.GameSettings;

        if (settings == null)
        {
            return Task.CompletedTask;
        }

        settings.SystemVolume = Math.Clamp(SystemVolume, 0, _maxVolume);
        settings.FurnitureVolume = Math.Clamp(FurnitureVolume, 0, _maxVolume);
        settings.TraxVolume = Math.Clamp(TraxVolume, 0, _maxVolume);

        var playerId = client.Player!.Player.Id;
        var system = settings.SystemVolume;
        var furniture = settings.FurnitureVolume;
        var trax = settings.TraxVolume;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.Set<PlayerGameSettings>()
                .Where(x => x.PlayerId == playerId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.SystemVolume, system)
                    .SetProperty(x => x.FurnitureVolume, furniture)
                    .SetProperty(x => x.TraxVolume, trax));
        };

        return Task.CompletedTask;
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
