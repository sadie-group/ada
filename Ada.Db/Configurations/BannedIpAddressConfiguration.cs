using Ada.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations;

public class BannedIpAddressConfiguration : IEntityTypeConfiguration<BannedIpAddress>
{
    public void Configure(EntityTypeBuilder<BannedIpAddress> entity)
    {
        entity.Property(x => x.IpAddress)
            .HasMaxLength(45);

        entity.HasIndex(x => x.IpAddress);
    }
}
