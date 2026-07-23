using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models.Groups;

namespace Ada.Db.Configurations.Groups;

public class GroupForumThreadConfiguration : IEntityTypeConfiguration<GroupForumThread>
{
    public void Configure(EntityTypeBuilder<GroupForumThread> entity)
    {
        entity.ToTable("group_forum_threads");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => x.GroupId);

        entity
            .HasOne(x => x.Group)
            .WithMany(x => x.ForumThreads)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(x => x.Player)
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
