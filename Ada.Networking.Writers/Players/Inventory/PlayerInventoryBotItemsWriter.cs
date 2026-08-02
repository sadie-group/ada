using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Inventory;

[PacketId(ServerPacketId.PlayerInventoryBotItems)]
public class PlayerInventoryBotItemsWriter : AbstractPacketWriter
{
    public required ICollection<PlayerBotDto> Bots { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Bots.Count);

        foreach (var bot in Bots)
        {
            writer.WriteInteger(bot.Id);
            writer.WriteString(bot.Username ?? string.Empty);
            writer.WriteString(bot.Motto ?? string.Empty);
            writer.WriteString(bot.Gender == PlayerAvatarGender.Male ? "m" : "f");
            writer.WriteString(bot.FigureCode ?? string.Empty);
        }
    }
}