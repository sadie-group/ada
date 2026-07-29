using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.PetRespect)]
public class RoomPetRespectWriter : AbstractPacketWriter
{
    public required int RespectType { get; init; }
    public required PlayerPetDto Pet { get; init; }

    public override void OnConfigureRules()
    {
        Override(nameof(RespectType), writer =>
        {
            writer.WriteInteger(RespectType);
            writer.WriteInteger(100);
        });
        Override(nameof(Pet), writer => PetSerializer.Serialize(writer, Pet));
    }
}
