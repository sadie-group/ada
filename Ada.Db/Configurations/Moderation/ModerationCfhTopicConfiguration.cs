using Ada.Db.Models.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations.Moderation;

public class ModerationCfhTopicConfiguration : IEntityTypeConfiguration<ModerationCfhTopic>
{
    public void Configure(EntityTypeBuilder<ModerationCfhTopic> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CategoryName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.CategoryName);

        builder.HasData(
            new ModerationCfhTopic { Id = 1, CategoryName = "Bullying", Name = "Verbal abuse", Order = 1 },
            new ModerationCfhTopic { Id = 2, CategoryName = "Bullying", Name = "Threats", Order = 2 },
            new ModerationCfhTopic { Id = 3, CategoryName = "Bullying", Name = "Harassment", Order = 3 },
            new ModerationCfhTopic { Id = 4, CategoryName = "Scamming", Name = "Trade scam", Order = 4 },
            new ModerationCfhTopic { Id = 5, CategoryName = "Scamming", Name = "Password phishing", Order = 5 },
            new ModerationCfhTopic { Id = 6, CategoryName = "Scamming", Name = "Account theft", Order = 6 },
            new ModerationCfhTopic { Id = 7, CategoryName = "Inappropriate", Name = "Offensive language", Order = 7 },
            new ModerationCfhTopic { Id = 8, CategoryName = "Inappropriate", Name = "Offensive room", Order = 8 },
            new ModerationCfhTopic { Id = 9, CategoryName = "Inappropriate", Name = "Offensive name or motto", Order = 9 },
            new ModerationCfhTopic { Id = 10, CategoryName = "Other", Name = "Room flooding", Order = 10 },
            new ModerationCfhTopic { Id = 11, CategoryName = "Other", Name = "Bot or scripting", Order = 11 },
            new ModerationCfhTopic { Id = 12, CategoryName = "Other", Name = "Something else", Order = 12 });
    }
}
