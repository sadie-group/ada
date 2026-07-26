using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Ada.API;

namespace Ada.Networking.Packets;

public class NetworkPacketWriter : INetworkPacketWriter
{
    private readonly ArrayBufferWriter<byte> _packet = new();

    public void WriteString(string data)
    {
        var count = Encoding.UTF8.GetByteCount(data);
        WriteShort((short) count);

        var span = _packet.GetSpan(count);
        Encoding.UTF8.GetBytes(data, span);
        _packet.Advance(count);
    }

    public void WriteShort(short data)
    {
        var span = _packet.GetSpan(sizeof(short));
        BinaryPrimitives.WriteInt16BigEndian(span, data);
        _packet.Advance(sizeof(short));
    }

    public void WriteInteger(int data)
    {
        var span = _packet.GetSpan(sizeof(int));
        BinaryPrimitives.WriteInt32BigEndian(span, data);
        _packet.Advance(sizeof(int));
    }

    public void WriteLong(long data) => WriteInteger((int) data);

    public void WriteBool(bool boolean)
    {
        var span = _packet.GetSpan(1);
        span[0] = (byte) (boolean ? 1 : 0);
        _packet.Advance(1);
    }

    public void WriteByte(byte b)
    {
        var span = _packet.GetSpan(1);
        span[0] = b;
        _packet.Advance(1);
    }

    private byte[]? _framedBytes;

    public byte[] GetAllBytes()
    {
        if (_framedBytes != null)
        {
            return _framedBytes;
        }

        var payloadLength = _packet.WrittenCount;
        var result = new byte[sizeof(int) + payloadLength];

        BinaryPrimitives.WriteInt32BigEndian(result, payloadLength);
        _packet.WrittenSpan.CopyTo(result.AsSpan(sizeof(int)));

        return _framedBytes = result;
    }
}