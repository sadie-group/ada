using Ada.Db.Models.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations;

public class ServerRoomConstantsConfiguration
    : IEntityTypeConfiguration<ServerRoomConstants>
{
    public void Configure(EntityTypeBuilder<ServerRoomConstants> entity)
    {
        entity.HasNoKey();
        entity.ToTable("server_room_constants");
    }
}