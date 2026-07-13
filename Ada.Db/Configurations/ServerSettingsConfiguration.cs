using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models.Server;

namespace Ada.Db.Configurations;

public class ServerSettingsConfiguration : IEntityTypeConfiguration<ServerSettings>
{
    public void Configure(EntityTypeBuilder<ServerSettings> entity)
    {
        entity.HasNoKey();
    }
}