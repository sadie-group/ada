using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.SaveNavigatorSettings)]
public class SaveNavigatorSettingsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper,
    ILogger<SaveNavigatorSettingsEventHandler> logger) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int WindowX { get; set; }
    public int WindowY { get; set; }
    public int WindowWidth { get; set; }
    public int WindowHeight { get; set; }
    public bool OpenSearches { get; set; }
    public int Mode { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player?.Player.NavigatorSettings == null)
        {
            return;
        }
        
        var navigatorSettings = player.Player.NavigatorSettings;
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var entity = await dbContext.PlayerNavigatorSettings
            .FirstOrDefaultAsync(x => x.PlayerId == player.Player.Id);

        if (entity == null)
        {
            #pragma warning disable CA1873
            logger.LogWarning("PlayerNavigatorSettings missing for player {PlayerId}", player.Player.Id);
            #pragma warning restore CA1873
            return;
        }

        mapper.Map(navigatorSettings, entity);
        await dbContext.SaveChangesAsync();
    }
}