using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models.Constants;

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