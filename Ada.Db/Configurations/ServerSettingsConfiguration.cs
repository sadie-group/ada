using Ada.Db.Models.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations;

public class ServerSettingsConfiguration : IEntityTypeConfiguration<ServerSettings>
{
    public void Configure(EntityTypeBuilder<ServerSettings> entity)
    {
        entity.HasNoKey();
    }
}