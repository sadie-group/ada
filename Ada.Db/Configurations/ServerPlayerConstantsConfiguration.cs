using Ada.Db.Models.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations;

public class ServerPlayerConstantsConfiguration
    : IEntityTypeConfiguration<ServerPlayerConstants>
{
    public void Configure(EntityTypeBuilder<ServerPlayerConstants> entity)
    {
        entity.HasNoKey();
        entity.ToTable("server_player_constants");
    }
}