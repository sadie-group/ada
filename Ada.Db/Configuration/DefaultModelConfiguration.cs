using Microsoft.EntityFrameworkCore;

namespace Ada.Db.Configuration;

public class DefaultModelConfiguration : IModelConfiguration
{
    public bool UseSnakeCaseNamingConvention => true;

    public void Apply(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdaDbContext).Assembly);
}
