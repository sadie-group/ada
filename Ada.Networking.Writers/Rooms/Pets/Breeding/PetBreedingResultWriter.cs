using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets.Breeding;

[PacketId(ServerPacketId.PetBreedingResult)]
public class PetBreedingResultWriter : AbstractPacketWriter
{
    public required int NestId { get; init; }
    public required int PetType { get; init; }
    public required PlayerPetDto PetOne { get; init; }
    public required PlayerPetDto PetTwo { get; init; }
    public required IReadOnlyDictionary<int, (int Percentage, IReadOnlyList<int> Breeds)> RarityLevels { get; init; }

    public override void OnConfigureRules()
    {
        Override(nameof(NestId), writer => writer.WriteInteger(NestId));
        Override(nameof(PetType), _ => { });
        Override(nameof(PetOne), writer => WriteBreedingPet(writer, PetOne));
        Override(nameof(PetTwo), writer => WriteBreedingPet(writer, PetTwo));
        Override(nameof(RarityLevels), writer =>
        {
            writer.WriteInteger(RarityLevels.Count);

            foreach (var level in RarityLevels.OrderByDescending(x => x.Key))
            {
                writer.WriteInteger(level.Value.Percentage);
                writer.WriteInteger(level.Value.Breeds.Count);

                foreach (var breed in level.Value.Breeds)
                {
                    writer.WriteInteger(breed);
                }
            }

            writer.WriteInteger(PetType);
        });
    }

    private static void WriteBreedingPet(INetworkPacketWriter writer, PlayerPetDto pet)
    {
        writer.WriteInteger(pet.Id);
        writer.WriteString(pet.Name ?? "");
        writer.WriteInteger(pet.Level);
        writer.WriteString(pet.Color ?? "");
        writer.WriteString(pet.OwnerName);
    }
}
