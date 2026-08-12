using Ada.Db.Models.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations.Groups;

public class GroupForumMessageConfiguration : IEntityTypeConfiguration<GroupForumMessage>
{
    public void Configure(EntityTypeBuilder<GroupForumMessage> entity)
    {
        entity.ToTable("group_forum_messages");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.ThreadId);

        entity.Property(x => x.Message).HasColumnType("text");

        entity
            .HasOne(x => x.Thread)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(x => x.Player)
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
