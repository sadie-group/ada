using Ada.Db.Models.Moderation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations.Moderation;

public class ModerationTicketConfiguration : IEntityTypeConfiguration<ModerationTicket>
{
    public void Configure(EntityTypeBuilder<ModerationTicket> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).HasMaxLength(512).IsRequired();
        builder.Property(x => x.State).HasConversion<int>();
        builder.Property(x => x.Resolution).HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.State);
        builder.HasIndex(x => x.ReporterPlayerId);
        builder.HasIndex(x => x.ReportedPlayerId);

        builder.HasOne(x => x.ReporterPlayer)
            .WithMany()
            .HasForeignKey(x => x.ReporterPlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ReportedPlayer)
            .WithMany()
            .HasForeignKey(x => x.ReportedPlayerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.PickedByPlayer)
            .WithMany()
            .HasForeignKey(x => x.PickedByPlayerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Room)
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
