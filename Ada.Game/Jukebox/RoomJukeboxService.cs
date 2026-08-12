using Ada.API.DTOs.Jukebox;
using Ada.API.Interfaces.Game.Jukebox;
using Ada.Db;
using Ada.Db.Models.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Jukebox;

public class RoomJukeboxService(IDbContextFactory<AdaDbContext> dbContextFactory) : IRoomJukeboxService
{
    public int MaxTracksPerRoom => 20;

    public async Task<IReadOnlyList<RoomJukeboxTrackDto>> GetPlaylistAsync(int roomId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        return await dbContext.RoomJukeboxTracks
            .AsNoTracking()
            .Where(x => x.RoomId == roomId)
            .OrderBy(x => x.OrderIndex)
            .Select(x => new RoomJukeboxTrackDto
            {
                Id = x.Id,
                PlayerFurnitureItemId = x.PlayerFurnitureItemId,
                SoundTrackId = x.SoundTrackId,
                OrderIndex = x.OrderIndex
            })
            .ToListAsync();
    }

    public async Task<bool> TryAddAsync(int roomId, long playerId, int playerFurnitureItemId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var disc = await dbContext.PlayerFurnitureItems
            .AsNoTracking()
            .Where(x => x.Id == playerFurnitureItemId && x.PlayerId == playerId)
            .Select(x => new { x.Id, x.MetaData })
            .FirstOrDefaultAsync();

        if (disc == null || !int.TryParse(disc.MetaData, out var soundTrackId))
        {
            return false;
        }

        if (!await dbContext.SoundTracks.AnyAsync(x => x.Id == soundTrackId))
        {
            return false;
        }

        if (await dbContext.RoomJukeboxTracks.AnyAsync(x => x.PlayerFurnitureItemId == disc.Id))
        {
            return false;
        }

        var used = await dbContext.RoomJukeboxTracks
            .Where(x => x.RoomId == roomId)
            .Select(x => x.OrderIndex)
            .ToListAsync();

        if (used.Count >= MaxTracksPerRoom)
        {
            return false;
        }

        var orderIndex = Enumerable
            .Range(0, MaxTracksPerRoom)
            .First(index => !used.Contains(index));

        dbContext.RoomJukeboxTracks.Add(new RoomJukeboxTrack
        {
            RoomId = roomId,
            PlayerFurnitureItemId = disc.Id,
            SoundTrackId = soundTrackId,
            OrderIndex = orderIndex,
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<int?> TryRemoveAsync(int roomId, int orderIndex)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var track = await dbContext.RoomJukeboxTracks
            .FirstOrDefaultAsync(x => x.RoomId == roomId && x.OrderIndex == orderIndex);

        if (track == null)
        {
            return null;
        }

        dbContext.RoomJukeboxTracks.Remove(track);

        await dbContext.SaveChangesAsync();

        return track.PlayerFurnitureItemId;
    }
}
