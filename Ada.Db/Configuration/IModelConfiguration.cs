using Microsoft.EntityFrameworkCore;

namespace Ada.Db.Configuration;

public interface IModelConfiguration
{
    bool UseSnakeCaseNamingConvention { get; }

    void Apply(ModelBuilder modelBuilder);
}
