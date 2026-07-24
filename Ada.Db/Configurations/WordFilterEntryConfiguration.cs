using Ada.Db.Models.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations;

public class WordFilterEntryConfiguration : IEntityTypeConfiguration<WordFilterEntry>
{
    public void Configure(EntityTypeBuilder<WordFilterEntry> entity)
    {
        entity.ToTable("word_filter_entries");
        entity.Property(x => x.Pattern).HasMaxLength(255);
        entity.Property(x => x.Replacement).HasMaxLength(255);
    }
}
