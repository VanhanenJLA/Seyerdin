namespace Seyerdin.ServerModernized.Protocol;

public static class LegacyProtocolCodec
{
    public static bool TryReadPacket(List<byte> buffer, byte packetsSent, out LegacyPacket? packet)
    {
        packet = null;

        if (buffer.Count < 3)
        {
            return false;
        }

        var payloadLength = (buffer[0] << 8) | buffer[1];
        if (payloadLength < 1)
        {
            throw new InvalidDataException("Legacy packet length must include the packet body and checksum.");
        }

        var frameLength = 2 + payloadLength;
        if (buffer.Count < frameLength)
        {
            return false;
        }

        var frame = buffer.GetRange(2, payloadLength).ToArray();
        buffer.RemoveRange(0, frameLength);

        if (frame.Length < 2)
        {
            throw new InvalidDataException("Legacy packet frame is too short.");
        }

        var checksumByte = frame[^1];
        var packetId = frame[0];
        var payload = frame[1..^1];

        ValidateChecksum(packetId, payload, checksumByte, packetsSent);
        packet = new LegacyPacket(packetId, payload, checksumByte);
        return true;
    }

    public static byte[] CreateClientPacket(byte packetId, ReadOnlySpan<byte> payload, byte packetsSent)
    {
        return CreateFrame(packetId, payload, packetsSent, includeChecksum: true);
    }

    public static byte[] CreateServerRawFrame(ReadOnlySpan<byte> payload)
    {
        var frame = new byte[payload.Length + 2];
        frame[0] = (byte)(payload.Length / 256);
        frame[1] = (byte)(payload.Length % 256);
        payload.CopyTo(frame.AsSpan(2));
        return frame;
    }

    private static byte[] CreateFrame(byte packetId, ReadOnlySpan<byte> payload, byte packetsSent, bool includeChecksum)
    {
        var bodyLength = 1 + payload.Length + (includeChecksum ? 1 : 0);
        var frame = new byte[2 + bodyLength];
        frame[0] = (byte)(bodyLength / 256);
        frame[1] = (byte)(bodyLength % 256);
        frame[2] = packetId;
        payload.CopyTo(frame.AsSpan(3));

        if (includeChecksum)
        {
            frame[^1] = CalculateChecksum(frame.AsSpan(2, 1 + payload.Length), packetsSent);
        }

        return frame;
    }

    private static void ValidateChecksum(byte packetId, byte[] payload, byte actualChecksum, byte packetsSent)
    {
        Span<byte> body = stackalloc byte[1 + payload.Length];
        body[0] = packetId;
        payload.CopyTo(body[1..]);

        var expectedChecksum = CalculateChecksum(body, packetsSent);
        if (expectedChecksum != actualChecksum)
        {
            throw new InvalidDataException(
                $"Legacy packet checksum mismatch for packet {packetId}. Expected {expectedChecksum}, got {actualChecksum}.");
        }
    }

    private static byte CalculateChecksum(ReadOnlySpan<byte> body, byte packetsSent)
    {
        var calc = 0;
        foreach (var value in body)
        {
            calc += value + 7;
        }

        var sum = (byte)(calc % 256);
        var crypt = (byte)(sum ^ (packetsSent + 1));
        return (byte)~crypt;
    }
}
