using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Db.Models.Players;

namespace Ada.Db.Configurations.Players;

public class PlayerRoomBanConfiguration : IEntityTypeConfiguration<PlayerRoomBan>
{
    public void Configure(EntityTypeBuilder<PlayerRoomBan> entity)
    {
        entity.ToTable("player_room_bans");
    }
}