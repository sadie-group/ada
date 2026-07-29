using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.PetHorseFigure)]
public class RoomPetHorseFigureWriter : AbstractPacketWriter
{
    public required PlayerPetDto Pet { get; init; }

    public override void OnConfigureRules()
    {
        Override(nameof(Pet), writer =>
        {
            writer.WriteInteger(Pet.Id);
            writer.WriteInteger(Pet.Id);
            writer.WriteInteger(Pet.Type);
            writer.WriteInteger(Pet.Race);
            writer.WriteString((Pet.Color ?? "").ToLower());

            if (Pet.HasSaddle)
            {
                writer.WriteInteger(2);
                writer.WriteInteger(3);
                writer.WriteInteger(4);
                writer.WriteInteger(9);
                writer.WriteInteger(0);
                writer.WriteInteger(3);
                writer.WriteInteger(Pet.HairStyle);
                writer.WriteInteger(Pet.HairColor);
                writer.WriteInteger(3);
                writer.WriteInteger(Pet.HairStyle);
                writer.WriteInteger(Pet.HairColor);
            }
            else
            {
                writer.WriteInteger(1);
                writer.WriteInteger(2);
                writer.WriteInteger(2);
                writer.WriteInteger(Pet.HairStyle);
                writer.WriteInteger(Pet.HairColor);
                writer.WriteInteger(3);
                writer.WriteInteger(Pet.HairStyle);
                writer.WriteInteger(Pet.HairColor);
            }

            writer.WriteBool(Pet.HasSaddle);
            writer.WriteBool(false);
        });
    }
}
