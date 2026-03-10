using System.Buffers.Binary;
using Seyerdin.ServerModernized.Domain;

namespace Seyerdin.ServerModernized.Protocol;

public static class LegacyWorldPacketFactory
{
    public const int LegacyMapLength = 2379;

    public static byte[] BuildJoinedGameBody()
    {
        return new[] { (byte)24 };
    }

    public static byte[] BuildHourBody(byte hour)
    {
        return new[] { (byte)110, hour };
    }

    public static byte[] BuildJoinedMapBody(CharacterRecord character)
    {
        Span<byte> body = stackalloc byte[15];
        body[0] = 12;
        BinaryPrimitives.WriteUInt16BigEndian(body[1..3], unchecked((ushort)Math.Max(character.MapId, (short)1)));
        body[3] = character.X;
        body[4] = character.Y;
        body[5] = character.Direction;
        body[6] = character.WalkCode;
        // Version/checksum are left at zero for now; the client will request the map body.
        return body.ToArray();
    }

    public static byte[] BuildMapDataBody()
    {
        var body = new byte[1 + LegacyMapLength];
        body[0] = 21;
        return body;
    }

    public static byte[] BuildDoneSendingMapBody()
    {
        return new[] { (byte)22 };
    }
}
