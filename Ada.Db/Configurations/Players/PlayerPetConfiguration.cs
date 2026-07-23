using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models.Players;

namespace Ada.Db.Configurations.Players;

public class PlayerPetConfiguration : IEntityTypeConfiguration<PlayerPet>
{
    public void Configure(EntityTypeBuilder<PlayerPet> entity)
    {
        entity.ToTable("player_pets");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.PlayerId);
        entity.HasIndex(x => x.RoomId);

        entity
            .HasOne<Player>()
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
