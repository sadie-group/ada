using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ada.Db;

public class AdaMigrationsDbContext(DbContextOptions<AdaDbContext> options) : AdaDbContext(options);

public class AdaMigrationsContextFactory : IDesignTimeDbContextFactory<AdaMigrationsDbContext>
{
    public AdaMigrationsDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("Default");
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        return new AdaMigrationsDbContext(new DbContextOptionsBuilder<AdaDbContext>()
            .UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                b => b.MigrationsAssembly("Ada.Db"))
            .UseSnakeCaseNamingConvention()
            .Options);
    }
}