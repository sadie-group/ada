using Ada.Db.Models.Rooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations.Rooms;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> entity)
    {
        entity.HasOne(r => r.PaintSettings)
            .WithOne(p => p.Room)
            .HasForeignKey<RoomPaintSettings>(p => p.RoomId);

        entity.HasMany(r => r.FurnitureItems)
            .WithOne(f => f.Room)
            .HasForeignKey(f => f.RoomId);

        entity.Navigation(r => r.Settings).AutoInclude();
    }
}