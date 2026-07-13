using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models.Rooms;

namespace Ada.Db.Configurations.Rooms;

public class RoomTagConfiguration : IEntityTypeConfiguration<RoomTag>
{
    public void Configure(EntityTypeBuilder<RoomTag> entity)
    {
        entity.ToTable("room_tags");
    }
}