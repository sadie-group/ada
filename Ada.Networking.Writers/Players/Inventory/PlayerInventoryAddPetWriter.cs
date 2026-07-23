using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Writers.Players.Inventory;

[PacketId(ServerPacketId.PlayerInventoryAddPet)]
public class PlayerInventoryAddPetWriter : AbstractPacketWriter
{
    public required PlayerPetDto Pet { get; init; }

    public override void OnConfigureRules()
    {
        Override(GetType().GetProperty(nameof(Pet))!, writer =>
        {
            PetSerializer.Serialize(writer, Pet);
            writer.WriteBool(false);
        });
    }
}
