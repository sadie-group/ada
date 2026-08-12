using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Pets;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Pets;

public class PlayerPetPersistence(IDbContextFactory<AdaDbContext> dbContextFactory) : IPlayerPetPersistence
{
    public async Task SaveRideSettingsAsync(PlayerPetDto pet)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerPets
            .Where(x => x.Id == pet.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.AnyoneCanRide, pet.AnyoneCanRide));
    }

    public async Task SaveSaddleAsync(PlayerPetDto pet)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerPets
            .Where(x => x.Id == pet.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.HasSaddle, pet.HasSaddle));
    }

    public async Task SaveBreedingAsync(PlayerPetDto pet)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerPets
            .Where(x => x.Id == pet.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.PubliclyBreedable, pet.PubliclyBreedable));
    }

    public async Task SaveScratchAsync(PlayerPetDto pet)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerPets
            .Where(x => x.Id == pet.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Respect, pet.Respect)
                .SetProperty(x => x.Happiness, pet.Happiness)
                .SetProperty(x => x.Experience, pet.Experience)
                .SetProperty(x => x.Level, pet.Level));
    }

    public async Task DeleteAsync(int petId)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.PlayerPets
            .Where(x => x.Id == petId)
            .ExecuteDeleteAsync();
    }
}
