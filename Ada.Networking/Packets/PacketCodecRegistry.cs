using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Ada.API.Interfaces.Networking.Packets;

namespace Ada.Networking.Packets;

public sealed class PacketCodecRegistry : IPacketCodecRegistry
{
    private readonly FrozenDictionary<string, IPacketCodec> _codecs;

    public PacketCodecRegistry(IEnumerable<IPacketCodec> codecs, string defaultRevision)
    {
        _codecs = codecs
            .GroupBy(c => c.Revision, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

        if (_codecs.Count == 0)
        {
            throw new InvalidOperationException("No packet codecs were registered");
        }

        if (!_codecs.TryGetValue(defaultRevision, out var fallback))
        {
            throw new InvalidOperationException(
                $"Default packet revision '{defaultRevision}' has no registered codec. " +
                $"Registered: {string.Join(", ", _codecs.Keys)}");
        }

        Default = fallback;
    }

    public IPacketCodec Default { get; }

    public IReadOnlyCollection<IPacketCodec> Codecs => _codecs.Values;

    public bool TryGet(string revision, [NotNullWhen(true)] out IPacketCodec? codec)
        => _codecs.TryGetValue(revision, out codec);
}
