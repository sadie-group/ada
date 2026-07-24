using Ada.API.DTOs;
using Ada.API.Interfaces.Game.Jukebox;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Jukebox;

public class SoundTrackRepository(IDbContextFactory<AdaDbContext> dbContextFactory) : ISoundTrackRepository
{
    public async Task<IReadOnlyList<SoundTrackDto>> GetByIdsAsync(IReadOnlyList<int> ids)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        return await db.SoundTracks
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .Select(x => new SoundTrackDto
            {
                Id = x.Id,
                Name = x.Name,
                Author = x.Author,
                Code = x.Code,
                Data = x.Data,
                Length = x.Length
            })
            .ToListAsync();
    }

    public async Task<SoundTrackDto?> GetByNameAsync(string name)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        return await db.SoundTracks
            .AsNoTracking()
            .Where(x => x.Name == name)
            .Select(x => new SoundTrackDto
            {
                Id = x.Id,
                Name = x.Name,
                Author = x.Author,
                Code = x.Code,
                Data = x.Data,
                Length = x.Length
            })
            .FirstOrDefaultAsync();
    }
}
