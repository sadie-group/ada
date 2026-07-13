using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models.Navigator;

namespace Ada.Db.Configurations;

public class NavigatorCategoryConfiguration : IEntityTypeConfiguration<NavigatorCategory>
{
    public void Configure(EntityTypeBuilder<NavigatorCategory> entity)
    {
        entity.HasOne(e => e.Tab)
            .WithMany(t => t.Categories)
            .HasForeignKey(e => e.TabId);
    }
}
