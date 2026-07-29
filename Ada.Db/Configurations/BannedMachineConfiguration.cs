using Ada.Db.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations;

public class BannedMachineConfiguration : IEntityTypeConfiguration<BannedMachine>
{
    public void Configure(EntityTypeBuilder<BannedMachine> entity)
    {
        entity.Property(x => x.MachineId)
            .HasMaxLength(255);

        entity.HasIndex(x => x.MachineId);
    }
}
