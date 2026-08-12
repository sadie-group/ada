using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Game.Players.Options;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ada.Game.Players;

public class PlayerLoaderService(IDbContextFactory<AdaDbContext> dbContextFactory,
    IOptions<PlayerOptions> playerOptions,
    IMapper mapper) : IPlayerLoaderService
{
    public async Task<PlayerSsoTokenDto?> GetTokenAsync(string token)
    {
        var digest = SsoTokenHasher.Hash(token);
        var acceptRaw = !playerOptions.Value.RequireHashedSsoTokens;
        var grace = TimeSpan.FromSeconds(Math.Clamp(playerOptions.Value.SsoGraceSeconds, 0, 60));
        var expires = DateTimeOffset.UtcNow.Subtract(grace);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var entity = await dbContext.PlayerSsoToken
            .FirstOrDefaultAsync(x =>
                (x.Token == digest || (acceptRaw && x.Token == token)) &&
                x.ExpiresAt >= expires &&
                x.UsedAt == null);

        if (entity == null)
        {
            return null;
        }

        if (playerOptions.Value.CanReuseSsoTokens)
        {
            return mapper.Map<PlayerSsoTokenDto>(entity);
        }

        var usedAt = DateTimeOffset.UtcNow;

        var claimed = await dbContext.PlayerSsoToken
            .Where(x => x.Id == entity.Id && x.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAt, usedAt));

        if (claimed == 0)
        {
            return null;
        }

        entity.UsedAt = usedAt;

        return mapper.Map<PlayerSsoTokenDto>(entity);
    }
}
