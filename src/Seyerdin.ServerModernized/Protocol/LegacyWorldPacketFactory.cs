using System.Buffers.Binary;
using Seyerdin.ServerModernized.Domain;

namespace Seyerdin.ServerModernized.Protocol;

public static class LegacyWorldPacketFactory
{
    public const int LegacyMapLength = 2379;
    private static readonly byte[] ShellMap = BuildShellMapData();
    private const int ShellMapVersion = 1;
    private static readonly int ShellMapChecksum = CalculateChecksum(ShellMap);

    public static byte[] BuildJoinedGameBody()
    {
        return new[] { (byte)24 };
    }

    public static byte[] BuildHourBody(byte hour)
    {
        return new[] { (byte)110, hour };
    }

    public static byte[] BuildJoinedMapBody(CharacterRecord character, LegacyMapDescriptor map)
    {
        Span<byte> body = stackalloc byte[15];
        body[0] = 12;
        BinaryPrimitives.WriteUInt16BigEndian(body[1..3], unchecked((ushort)Math.Max(character.MapId, (short)1)));
        body[3] = character.X;
        body[4] = character.Y;
        body[5] = character.Direction;
        body[6] = character.WalkCode;
        BinaryPrimitives.WriteUInt32BigEndian(body[7..11], unchecked((uint)map.Version));
        BinaryPrimitives.WriteUInt32BigEndian(body[11..15], unchecked((uint)map.Checksum));
        return body.ToArray();
    }

    public static byte[] BuildMapDataBody(LegacyMapDescriptor map)
    {
        var body = new byte[1 + LegacyMapLength];
        body[0] = 21;
        map.Data.CopyTo(body, 1);
        return body;
    }

    public static byte[] BuildDoneSendingMapBody()
    {
        return new[] { (byte)22 };
    }

    public static LegacyMapDescriptor CreateMapDescriptor(byte[] data)
    {
        if (data.Length != LegacyMapLength)
        {
            throw new ArgumentException($"Legacy map data must be exactly {LegacyMapLength} bytes.", nameof(data));
        }

        var version = ReadInt32BigEndian(data, 30);
        var checksum = CalculateChecksum(data);
        return new LegacyMapDescriptor(data, version, checksum);
    }

    public static byte[] CreateShellMapData()
    {
        var clone = new byte[ShellMap.Length];
        ShellMap.CopyTo(clone, 0);
        return clone;
    }

    private static byte[] BuildShellMapData()
    {
        var data = new byte[LegacyMapLength];

        WriteFixedString(data, 0, 30, "Modernized Shell");
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(30, 4), ShellMapVersion);
        data[44] = 0;
        data[45] = 1; // Boot map
        data[46] = 5;
        data[47] = 5;
        data[49] = 120; // ambient intensity

        for (var y = 0; y < 12; y++)
        {
            for (var x = 0; x < 12; x++)
            {
                var offset = 70 + (y * 192) + (x * 16);
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(offset, 2), 1);
                data[offset + 10] = 0; // walkable tile
                data[offset + 15] = 0;
            }
        }

        return data;
    }

    private static int CalculateChecksum(IEnumerable<byte> bytes)
    {
        var sum = 0;
        foreach (var value in bytes)
        {
            sum += value;
        }

        return sum;
    }

    private static void WriteFixedString(byte[] target, int offset, int length, string value)
    {
        var bytes = LegacyEncoding.GetBytes(value);
        Array.Copy(bytes, 0, target, offset, Math.Min(length, bytes.Length));
    }

    private static int ReadInt32BigEndian(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset, 4));
    }
}
