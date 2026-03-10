namespace Seyerdin.ServerModernized.Protocol;

public sealed record LegacyPacket(byte PacketId, byte[] Payload, byte ChecksumByte)
{
    public ReadOnlyMemory<byte> PayloadMemory => Payload;
}
