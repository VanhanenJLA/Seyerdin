using System.Buffers.Binary;

namespace Seyerdin.ServerModernized.Protocol;

public static class LegacyMapParser
{
    public static LegacyMapInfo Parse(byte[] data)
    {
        if (data.Length != LegacyWorldPacketFactory.LegacyMapLength)
        {
            throw new ArgumentException(
                $"Legacy map data must be exactly {LegacyWorldPacketFactory.LegacyMapLength} bytes.",
                nameof(data));
        }

        return new LegacyMapInfo(
            Name: LegacyEncoding.GetString(data.AsSpan(0, 30)).TrimEnd('\0', ' '),
            Version: BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(30, 4)),
            Checksum: CalculateChecksum(data),
            ExitUp: ReadInt16(data, 36),
            ExitDown: ReadInt16(data, 38),
            ExitLeft: ReadInt16(data, 40),
            ExitRight: ReadInt16(data, 42),
            BootMap: ReadInt16(data, 44),
            BootX: data[46],
            BootY: data[47],
            Intensity: data[49]);
    }

    private static short ReadInt16(byte[] data, int offset)
    {
        return unchecked((short)BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2)));
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
}
