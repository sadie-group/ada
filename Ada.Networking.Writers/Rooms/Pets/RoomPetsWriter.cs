using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.RoomUserData)]
public class RoomPetsWriter : AbstractPacketWriter
{
    public required ICollection<IRoomPet> Pets { get; init; }

    public override void OnConfigureRules()
    {
        Override(nameof(Pets), writer =>
        {
            writer.WriteInteger(Pets.Count);

            foreach (var roomPet in Pets)
            {
                var pet = roomPet.Pet;

                writer.WriteInteger(pet.Id);
                writer.WriteString(pet.Name ?? "");
                writer.WriteString("");
                writer.WriteString(PetHelpers.BuildLookString(pet.Type, pet.Race, pet.Color ?? "", pet.HasSaddle, pet.HairStyle, pet.HairColor));
                writer.WriteInteger(pet.Id);
                writer.WriteInteger(roomPet.Point.X);
                writer.WriteInteger(roomPet.Point.Y);
                writer.WriteString(roomPet.PointZ.ToString("0.0"));
                writer.WriteInteger(0);
                writer.WriteInteger(2);
                writer.WriteInteger(pet.Type);
                writer.WriteInteger((int) pet.PlayerId);
                writer.WriteString(pet.OwnerName);
                writer.WriteInteger(1);
                writer.WriteBool(pet.HasSaddle);
                writer.WriteBool(false);
                writer.WriteBool(false);
                writer.WriteBool(true);
                writer.WriteBool(false);
                writer.WriteBool(false);
                writer.WriteInteger(pet.Level);
                writer.WriteString("");
            }
        });
    }
}
