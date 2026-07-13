using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Helpers;
using Ada.Db.Models.Players;

namespace Ada.Db.Configurations.Players;

public class PlayerAvatarDataConfiguration : IEntityTypeConfiguration<PlayerAvatarData>
{
    public void Configure(EntityTypeBuilder<PlayerAvatarData> entity)
    {
        entity.Property(p => p.Gender)
            .HasConversion(
                v => EnumHelpers.GetEnumDescription(v),
                v => EnumHelpers.GetEnumValueFromDescription<PlayerAvatarGender>(v.ToUpper()));

        entity.Property(p => p.ChatBubbleId)
            .HasDefaultValue(ChatBubble.Default);
    }
}