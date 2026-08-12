using Ada.Db.Models.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ada.Db.Configurations.Players;

public class PlayerRoomBanConfiguration : IEntityTypeConfiguration<PlayerRoomBan>
{
    public void Configure(EntityTypeBuilder<PlayerRoomBan> entity)
    {
        entity.ToTable("player_room_bans");
    }
}