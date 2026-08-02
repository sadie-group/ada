using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Wardrobe;

[PacketId(ServerPacketId.PlayerWardrobe)]
public class PlayerWardrobeWriter : AbstractPacketWriter
{
    public required int State { get; init; }
    public required ICollection<PlayerWardrobeItemDto> Outfits { get; init; }

    public override void OnConfigureRules()
    {
        Override(nameof(Outfits), writer =>
        {
            writer.WriteInteger(Outfits.Count);

            var i = 0;
        
            foreach (var outfit in Outfits)
            {
                i++;
            
                writer.WriteInteger(i);
                writer.WriteString(outfit.FigureCode ?? string.Empty);
                writer.WriteString(outfit.Gender == PlayerAvatarGender.Male ? "M" : "F");
            }
        });
    }
}