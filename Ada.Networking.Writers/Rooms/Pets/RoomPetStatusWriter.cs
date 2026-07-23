using Ada.API;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Pets;

[PacketId(ServerPacketId.RoomUserStatus)]
public class RoomPetStatusWriter : AbstractPacketWriter
{
    public required ICollection<IRoomPet> Pets { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Pets.Count);

        foreach (var pet in Pets)
        {
            var statusList = pet.StatusMap
                .Select(x => x.Key + (string.IsNullOrEmpty(x.Value) ? "" : " " + x.Value));

            writer.WriteLong(pet.Pet.Id);
            writer.WriteInteger(pet.Point.X);
            writer.WriteInteger(pet.Point.Y);
            writer.WriteString(pet.PointZ.ToString("0.00"));
            writer.WriteInteger((int) pet.DirectionHead);
            writer.WriteInteger((int) pet.Direction);
            writer.WriteString("/" + string.Join("/", statusList).TrimEnd('/'));
        }
    }
}
