using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.PetTrainingPanel)]
public class PetTrainingPanelWriter : AbstractPacketWriter
{
    public required int PetId { get; init; }
    public required IReadOnlyList<int> CommandIds { get; init; }
    public required IReadOnlyList<int> EnabledCommandIds { get; init; }

    public override void OnConfigureRules()
    {
        Override(GetType().GetProperty(nameof(CommandIds))!, writer =>
        {
            writer.WriteInteger(CommandIds.Count);

            foreach (var id in CommandIds)
            {
                writer.WriteInteger(id);
            }
        });
        Override(GetType().GetProperty(nameof(EnabledCommandIds))!, writer =>
        {
            writer.WriteInteger(EnabledCommandIds.Count);

            foreach (var id in EnabledCommandIds)
            {
                writer.WriteInteger(id);
            }
        });
    }
}
