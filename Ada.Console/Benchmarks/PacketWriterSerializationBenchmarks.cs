using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets.Serialization;
using Ada.Networking.Writers.Generated;
using Ada.Networking.Writers.Players.Subscriptions;
using BenchmarkDotNet.Attributes;

namespace Ada.Console.Benchmarks;

[MemoryDiagnoser]
public class PacketWriterSerializationBenchmarks
{
    private sealed class SmallReflectedWriter : AbstractPacketWriter
    {
        public int UserId { get; init; }
        public string Position { get; init; } = "";
        public int Direction { get; init; }
    }

    private sealed class SmallManualWriter : AbstractPacketWriter
    {
        public int UserId { get; init; }
        public string Position { get; init; } = "";
        public int Direction { get; init; }

        public override void OnSerialize(INetworkPacketWriter writer)
        {
            writer.WriteInteger(UserId);
            writer.WriteString(Position);
            writer.WriteInteger(Direction);
        }
    }

    private sealed class WideReflectedWriter : AbstractPacketWriter
    {
        public int Id { get; init; }
        public int AssetId { get; init; }
        public int PositionX { get; init; }
        public int PositionY { get; init; }
        public int Direction { get; init; }
        public string StackHeight { get; init; } = "";
        public int Extra { get; init; }
        public int ObjectDataKey { get; init; }
        public string MetaData { get; init; } = "";
        public int Expires { get; init; }
        public int InteractionModes { get; init; }
        public long OwnerId { get; init; }
        public string OwnerUsername { get; init; } = "";
        public bool Hidden { get; init; }
    }

    private sealed class WideManualWriter : AbstractPacketWriter
    {
        public int Id { get; init; }
        public int AssetId { get; init; }
        public int PositionX { get; init; }
        public int PositionY { get; init; }
        public int Direction { get; init; }
        public string StackHeight { get; init; } = "";
        public int Extra { get; init; }
        public int ObjectDataKey { get; init; }
        public string MetaData { get; init; } = "";
        public int Expires { get; init; }
        public int InteractionModes { get; init; }
        public long OwnerId { get; init; }
        public string OwnerUsername { get; init; } = "";
        public bool Hidden { get; init; }

        public override void OnSerialize(INetworkPacketWriter writer)
        {
            writer.WriteInteger(Id);
            writer.WriteInteger(AssetId);
            writer.WriteInteger(PositionX);
            writer.WriteInteger(PositionY);
            writer.WriteInteger(Direction);
            writer.WriteString(StackHeight);
            writer.WriteInteger(Extra);
            writer.WriteInteger(ObjectDataKey);
            writer.WriteString(MetaData);
            writer.WriteInteger(Expires);
            writer.WriteInteger(InteractionModes);
            writer.WriteLong(OwnerId);
            writer.WriteString(OwnerUsername);
            writer.WriteBool(Hidden);
        }
    }

    private readonly BenchmarkCodec _codec = new();

    private SmallReflectedWriter _smallReflected = null!;
    private SmallManualWriter _smallManual = null!;
    private WideReflectedWriter _wideReflected = null!;
    private WideManualWriter _wideManual = null!;
    private PlayerSubscriptionWriter _realWriter = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallReflected = new SmallReflectedWriter { UserId = 42, Position = "5,5,0.0", Direction = 4 };
        _smallManual = new SmallManualWriter { UserId = 42, Position = "5,5,0.0", Direction = 4 };

        _wideReflected = new WideReflectedWriter
        {
            Id = 1, AssetId = 2, PositionX = 3, PositionY = 4, Direction = 5,
            StackHeight = "0.0", Extra = 1, ObjectDataKey = 0, MetaData = "1",
            Expires = -1, InteractionModes = 1, OwnerId = 99, OwnerUsername = "alice", Hidden = false
        };

        _wideManual = new WideManualWriter
        {
            Id = 1, AssetId = 2, PositionX = 3, PositionY = 4, Direction = 5,
            StackHeight = "0.0", Extra = 1, ObjectDataKey = 0, MetaData = "1",
            Expires = -1, InteractionModes = 1, OwnerId = 99, OwnerUsername = "alice", Hidden = false
        };

        _realWriter = new PlayerSubscriptionWriter
        {
            Name = "habbo_club", DaysLeft = 30, MemberPeriods = 3, PeriodsSubscribedAhead = 0,
            ResponseType = 1, HasEverBeenMember = true, IsVip = true, PastClubDays = 100,
            PastVipDays = 50, MinutesTillExpire = 4000, MinutesSinceModified = 10
        };
    }

    [Benchmark(Baseline = true, Description = "3 fields - reflected property mapping")]
    public INetworkPacketWriter SmallReflected()
        => NetworkPacketWriterSerializer.Serialize(_smallReflected, _codec);

    [Benchmark(Description = "3 fields - manual OnSerialize")]
    public INetworkPacketWriter SmallManual()
        => NetworkPacketWriterSerializer.Serialize(_smallManual, _codec);

    [Benchmark(Description = "14 fields - reflected property mapping")]
    public INetworkPacketWriter WideReflected()
        => NetworkPacketWriterSerializer.Serialize(_wideReflected, _codec);

    [Benchmark(Description = "14 fields - manual OnSerialize")]
    public INetworkPacketWriter WideManual()
        => NetworkPacketWriterSerializer.Serialize(_wideManual, _codec);

    [Benchmark(Description = "11 fields (real writer) - reflected property mapping")]
    public INetworkPacketWriter RealReflected()
    {
        PacketWriterFastPath.Handler = null;
        return NetworkPacketWriterSerializer.Serialize(_realWriter, _codec);
    }

    [Benchmark(Description = "11 fields (real writer) - source-generated fast path")]
    public INetworkPacketWriter RealGenerated()
    {
        PacketWriterFastPath.Handler = GeneratedPacketWriters.TrySerialize;
        return NetworkPacketWriterSerializer.Serialize(_realWriter, _codec);
    }
}
