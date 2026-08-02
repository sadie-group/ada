using System.Diagnostics.CodeAnalysis;

namespace Ada.API.Interfaces.Networking.Packets;

public interface IPacketCodecRegistry
{
    IPacketCodec Default { get; }
    IReadOnlyCollection<IPacketCodec> Codecs { get; }
    bool TryGet(string revision, [NotNullWhen(true)] out IPacketCodec? codec);
}
