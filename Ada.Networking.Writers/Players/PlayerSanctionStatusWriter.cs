using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players;

[PacketId(ServerPacketId.PlayerSanctionStatus)]
public class PlayerSanctionStatusWriter : AbstractPacketWriter
{
    public required bool HasPreviousSanction { get; init; }
    public required bool OnProbation { get; init; }
    public required string SanctionName { get; init; }
    public required int SanctionLengthHours { get; init; }
    public required int Unknown1 { get; init; }
    public required string Reason { get; init; }
    public required DateTime ProbationStart { get; init; }
    public required int Unknown2 { get; init; }
    public required string NextSanctionType { get; init; }
    public required int HoursForNextSanction { get; init; }
    public required int Unknown3 { get; init; }
    public required bool Muted { get; init; }
    public required DateTime TradeLockedUntil { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteBool(HasPreviousSanction);
        writer.WriteBool(OnProbation);
        writer.WriteString(SanctionName ?? "");
        writer.WriteInteger(SanctionLengthHours);
        writer.WriteInteger(Unknown1);
        writer.WriteString(Reason ?? "");
        writer.WriteString(ProbationStart.ToString());
        writer.WriteInteger(Unknown2);
        writer.WriteString(NextSanctionType ?? "");
        writer.WriteInteger(HoursForNextSanction);
        writer.WriteInteger(Unknown3);
        writer.WriteBool(Muted);
        writer.WriteString(TradeLockedUntil == DateTime.MinValue ? "" : TradeLockedUntil.ToString());
    }
}