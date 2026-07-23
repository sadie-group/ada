using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.PetLevelUp)]
public class PetLevelUpWriter : AbstractPacketWriter
{
    public required PlayerPetDto Pet { get; init; }

    public override void OnConfigureRules()
    {
        Override(GetType().GetProperty(nameof(Pet))!, writer =>
        {
            writer.WriteInteger(Pet.Id);
            writer.WriteString(Pet.Name ?? "");
            writer.WriteInteger(Pet.Level);
            writer.WriteInteger(Pet.Type);
            writer.WriteInteger(Pet.Race);
            writer.WriteString(Pet.Color ?? "");
            writer.WriteInteger(0);
            writer.WriteInteger(0);
        });
    }
}
