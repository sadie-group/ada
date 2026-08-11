using System.Net;
using System.Net.WebSockets;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets;
using Ada.Networking.Packets.Serialization;
using BenchmarkDotNet.Attributes;

namespace Ada.Console.Benchmarks;

[MemoryDiagnoser]
public class BroadcastBenchmarks
{
    private sealed class CountingNetworkObject(IPacketCodec codec) : INetworkObject
    {
        public IPacketCodec Codec { get; set; } = codec;
        public IPAddress IpAddress { get; set; } = IPAddress.Loopback;
        public Guid Guid { get; set; } = Guid.NewGuid();
        public WebSocket WebSocket { get; set; } = null!;

        public int Queued { get; private set; }

        public void QueueOutbound(INetworkPacketWriter writer) => Queued++;

        public Task WriteToStreamAsync(AbstractPacketWriter writer) => Task.CompletedTask;

        public Task WriteToStreamAsync(INetworkPacketWriter writer)
        {
            Queued++;
            return Task.CompletedTask;
        }

        public Task FlushAsync() => Task.CompletedTask;
    }

    private sealed class StatusWriter : AbstractPacketWriter
    {
        public int UserId { get; init; }
        public string Position { get; init; } = "";
        public int Direction { get; init; }
    }

    private List<INetworkObject> _sameRevision = null!;
    private List<INetworkObject> _mixedRevision = null!;
    private StatusWriter _writer = null!;

    [Params(25, 50, 100)]
    public int Recipients { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var shared = new BenchmarkCodec();

        _sameRevision = Enumerable.Range(0, Recipients)
            .Select(INetworkObject (_) => new CountingNetworkObject(shared))
            .ToList();

        var second = new BenchmarkCodec();

        _mixedRevision = Enumerable.Range(0, Recipients)
            .Select(INetworkObject (i) => new CountingNetworkObject(i % 2 == 0 ? shared : second))
            .ToList();

        _writer = new StatusWriter { UserId = 42, Position = "5,5,0.0", Direction = 4 };
    }

    [Benchmark(Baseline = true, Description = "Serialize once, fan out to one revision")]
    public void SingleRevision() => PacketBroadcast.Queue(_writer, _sameRevision);

    [Benchmark(Description = "Two client revisions in the room (one serialization each)")]
    public void MixedRevision() => PacketBroadcast.Queue(_writer, _mixedRevision);

    [Benchmark(Description = "Naive: re-serialize per recipient (what most emulators do)")]
    public void SerializePerRecipient()
    {
        foreach (var recipient in _sameRevision)
        {
            recipient.QueueOutbound(NetworkPacketWriterSerializer.Serialize(_writer, recipient.Codec));
        }
    }
}
