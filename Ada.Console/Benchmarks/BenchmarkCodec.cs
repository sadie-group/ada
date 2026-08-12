using System.Diagnostics.CodeAnalysis;
using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets;

namespace Ada.Console.Benchmarks;

internal sealed class BenchmarkIdMap : IPacketIdMap
{
    public bool TryGetHandlerType(short packetId, [NotNullWhen(true)] out Type? handlerType)
    {
        handlerType = null;
        return false;
    }

    public bool TryGetOutgoingId(Type writerType, out short packetId)
    {
        packetId = 1;
        return true;
    }
}

internal sealed class BenchmarkCodec : IPacketCodec
{
    public string Revision => "BENCHMARK";

    public IPacketIdMap IdMap { get; } = new BenchmarkIdMap();

    public INetworkPacketDecoder Decoder { get; } = new NetworkPacketDecoder();

    public INetworkPacketWriter CreateWriter() => new NetworkPacketWriter();

    public INetworkPacketReader CreateReader(ReadOnlyMemory<byte> body) => new NetworkPacketReader(body);
}
