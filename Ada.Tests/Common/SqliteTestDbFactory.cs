using Ada.Db;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ada.Tests.Common;

public sealed class SqliteTestDbFactory : IDbContextFactory<AdaDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AdaDbContext> _options;

    public SqliteTestDbFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AdaDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public AdaDbContext CreateDbContext() => new SqliteAdaDbContext(_options);

    public void Dispose() => _connection.Dispose();
    
    private sealed class SqliteAdaDbContext(DbContextOptions<AdaDbContext> options) : AdaDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset))
                    {
                        property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
                    }
                    else if (property.ClrType == typeof(DateTimeOffset?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTimeOffset?, long?>(
                            v => v == null ? null : v.Value.ToUnixTimeMilliseconds(),
                            v => v == null ? null : DateTimeOffset.FromUnixTimeMilliseconds(v.Value)));
                    }
                }
            }
        }
    }
}
