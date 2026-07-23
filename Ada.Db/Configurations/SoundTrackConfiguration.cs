using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models;

namespace Ada.Db.Configurations;

public class SoundTrackConfiguration : IEntityTypeConfiguration<SoundTrack>
{
    public void Configure(EntityTypeBuilder<SoundTrack> entity)
    {
        entity.ToTable("soundtracks");

        entity.HasKey(x => x.Id);

        entity.Property(x => x.Data).HasColumnType("text");
        entity.HasIndex(x => x.Name);
    }
}
