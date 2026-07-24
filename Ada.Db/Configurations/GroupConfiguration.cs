using Ada.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> entity)
    {
        entity.ToTable("groups");

        entity.HasKey(x => x.Id);

        entity
            .HasMany(x => x.Players)
            .WithMany(x => x.Groups);
    }
}