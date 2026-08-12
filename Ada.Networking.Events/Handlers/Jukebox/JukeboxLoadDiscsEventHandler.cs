using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Jukebox;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Jukebox;

[PacketId(EventHandlerId.JukeboxLoadDiscs)]
public class JukeboxLoadDiscsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    private const string _discInteractionType = "song_disk";

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var discs = await dbContext.PlayerFurnitureItems
            .AsNoTracking()
            .Where(x => x.PlayerId == playerId &&
                        x.FurnitureItem!.InteractionType == _discInteractionType)
            .Select(x => new { x.Id, x.MetaData })
            .ToListAsync();

        var payload = new List<KeyValuePair<int, int>>();

        foreach (var disc in discs)
        {
            if (int.TryParse(disc.MetaData, out var soundTrackId))
            {
                payload.Add(new KeyValuePair<int, int>(disc.Id, soundTrackId));
            }
        }

        await client.WriteToStreamAsync(new JukeboxDiscsWriter
        {
            Discs = payload
        });
    }
}
