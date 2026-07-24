using Ada.Db.Models.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations.Players;

public class PlayerSsoTokenConfiguration : IEntityTypeConfiguration<PlayerSsoToken>
{
    public void Configure(EntityTypeBuilder<PlayerSsoToken> entity)
    {
        entity.ToTable("player_sso_tokens");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.PlayerId)
            .IsRequired();

        entity.Property(x => x.Token)
            .HasMaxLength(200);

        entity.Property(x => x.CreatedAt)
            .IsRequired();

        entity.Property(x => x.ExpiresAt)
            .IsRequired();

        entity.Property(x => x.UsedAt);
    }
}