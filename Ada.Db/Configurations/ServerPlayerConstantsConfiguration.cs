using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models.Constants;

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