using Microsoft.EntityFrameworkCore;

namespace Ada.Db;

public static class SeedData
{
    public static async Task SeedInitialDataAsync(AdaDbContext dbContext)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "seed.sql");
        var initialSql = await File.ReadAllTextAsync(path);
        await dbContext.Database.ExecuteSqlRawAsync(initialSql);
    }
}
