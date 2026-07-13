using Microsoft.EntityFrameworkCore;

namespace Ada.Db;

public static class SeedData
{
    public static async Task SeedInitialDataAsync(AdaDbContext dbContext)
    {
        using var httpClient = new HttpClient();
        var initialSql = await httpClient.GetStringAsync("https://cdn.ada.pw/seed.sql");
        await dbContext.Database.ExecuteSqlRawAsync(initialSql);
    }
}