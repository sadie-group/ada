using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.PetInformation)]
public class PetInformationWriter : AbstractPacketWriter
{
    private const int MonsterPlantTimeToLive = 3 * 24 * 60 * 60;

    public required PlayerPetDto Pet { get; init; }
    public required string OwnerName { get; init; }
    public required bool CanRide { get; init; }
    public required bool IsRiding { get; init; }

    public override void OnConfigureRules()
    {
        Override(nameof(OwnerName), _ => { });
        Override(nameof(CanRide), _ => { });
        Override(nameof(IsRiding), _ => { });
        Override(nameof(Pet), writer =>
        {
            var ageDays = (int) Math.Floor((DateTimeOffset.UtcNow - Pet.CreatedAt).TotalDays);

            writer.WriteInteger(Pet.Id);
            writer.WriteString(Pet.Name ?? "");
            writer.WriteInteger(Pet.Level);
            writer.WriteInteger(PetHelpers.MaximumLevel);
            writer.WriteInteger(Pet.Experience);
            writer.WriteInteger(PetHelpers.ExperienceGoalForLevel(Pet.Level, Pet.Experience));
            writer.WriteInteger(Pet.Energy);
            writer.WriteInteger(PetHelpers.MaxEnergyForLevel(Pet.Level));
            writer.WriteInteger(Pet.Happiness);
            writer.WriteInteger(100);
            writer.WriteInteger(Pet.Respect);
            writer.WriteInteger((int) Pet.PlayerId);
            writer.WriteInteger(ageDays + 1);
            writer.WriteString(OwnerName);
            writer.WriteInteger(0);
            writer.WriteBool(CanRide);
            writer.WriteBool(IsRiding);
            writer.WriteInteger(0);
            writer.WriteInteger(Pet.AnyoneCanRide ? 1 : 0);
            writer.WriteBool(false);
            writer.WriteBool(true);
            writer.WriteBool(false);
            writer.WriteInteger(0);
            writer.WriteInteger(MonsterPlantTimeToLive);
            writer.WriteInteger(0);
            writer.WriteInteger(0);
            writer.WriteBool(false);
        });
    }
}
