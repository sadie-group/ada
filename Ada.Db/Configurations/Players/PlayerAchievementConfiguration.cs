using Ada.Db.Models.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations.Players;

public class PlayerAchievementConfiguration : IEntityTypeConfiguration<PlayerAchievement>
{
    public void Configure(EntityTypeBuilder<PlayerAchievement> entity)
    {
        entity.ToTable("player_achievements");
        entity.HasKey(x => x.Id);
        entity.HasIndex(x => new { x.PlayerId, x.AchievementCode }).IsUnique();
        entity.Property(x => x.AchievementCode).HasMaxLength(64);
    }
}
