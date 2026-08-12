using Ada.Db.Models.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations.Groups;

public class GroupMembershipConfiguration : IEntityTypeConfiguration<GroupMembership>
{
    public void Configure(EntityTypeBuilder<GroupMembership> entity)
    {
        entity.ToTable("group_memberships");

        entity.HasKey(x => x.Id);

        entity.HasIndex(x => new { x.GroupId, x.PlayerId }).IsUnique();

        entity
            .HasOne(x => x.Group)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(x => x.Player)
            .WithMany()
            .HasForeignKey(x => x.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
