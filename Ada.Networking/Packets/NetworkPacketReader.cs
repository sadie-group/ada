using System.Buffers.Binary;
using System.Text;
using Ada.API.Interfaces.Networking.Packets;

namespace Ada.Networking.Packets;

public sealed class NetworkPacketReader(ReadOnlyMemory<byte> body) : INetworkPacketReader
{
    private readonly ReadOnlyMemory<byte> _body = body;
    private int _position;

    public int Remaining => _body.Length - _position;

    public int ReadInt()
        => BinaryPrimitives.ReadInt32BigEndian(Read(4));

    public short ReadShort()
        => BinaryPrimitives.ReadInt16BigEndian(Read(2));

    public bool ReadBool()
        => Read(1)[0] == 1;

    public byte ReadByte()
        => Read(1)[0];

    public string ReadString()
    {
        int length = ReadShort();

        if (length < 0)
        {
            throw new MalformedPacketException(
                $"String declared a negative length of {length} at offset {_position - 2}.");
        }

        return Encoding.UTF8.GetString(Read(length));
    }

    private ReadOnlySpan<byte> Read(int count)
    {
        if (count > Remaining)
        {
            throw new MalformedPacketException(
                $"Tried to read {count} byte(s) at offset {_position} with only {Remaining} remaining.");
        }

        var slice = _body.Span.Slice(_position, count);
        _position += count;
        return slice;
    }
}
