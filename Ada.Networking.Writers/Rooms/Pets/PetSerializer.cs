using Ada.API;
using Ada.API.DTOs.Players;

namespace Ada.Networking.Writers.Rooms.Pets;

public static class PetSerializer
{
    public static void Serialize(INetworkPacketWriter writer, PlayerPetDto pet)
    {
        writer.WriteInteger(pet.Id);
        writer.WriteString(pet.Name ?? "");
        writer.WriteInteger(pet.Type);
        writer.WriteInteger(pet.Race);
        writer.WriteString(pet.Color ?? "");
        writer.WriteInteger(0);
        writer.WriteInteger(0);
        writer.WriteInteger(0);
    }
}
