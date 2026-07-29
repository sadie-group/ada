using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.Pets;

namespace Ada.Networking.Writers.Players.Inventory;

[PacketId(ServerPacketId.PlayerInventoryPets)]
public class PlayerInventoryPetsWriter : AbstractPacketWriter
{
    public required ICollection<PlayerPetDto> Pets { get; init; }

    public override void OnConfigureRules()
    {
        Override(nameof(Pets), writer =>
        {
            writer.WriteInteger(1);
            writer.WriteInteger(1);
            writer.WriteInteger(Pets.Count);

            foreach (var pet in Pets)
            {
                PetSerializer.Serialize(writer, pet);
            }
        });
    }
}
