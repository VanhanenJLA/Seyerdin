using System.Buffers.Binary;
using Seyerdin.ServerModernized.Domain;

namespace Seyerdin.ServerModernized.Protocol;

public sealed class LegacyBootstrapPacketFactory
{
    private readonly LegacyContentCatalog content;

    public LegacyBootstrapPacketFactory(LegacyContentCatalog content)
    {
        this.content = content;
    }

    public byte[] BuildDataChunk(short startIndex)
    {
        var packets = new List<byte[]>();
        short nextStart = 1;
        var hasMore = false;

        for (short i = startIndex; i <= 255; i++)
        {
            AppendIfPresent(packets, EncodeNpc(i));
            AppendIfPresent(packets, EncodeHall(i));
            AppendIfPresent(packets, EncodeGuild(i));
            AppendIfPresent(packets, EncodePrefix(i));
            AppendIfPresent(packets, EncodeLight(i));

            if (EstimatePayloadLength(packets) >= 1024)
            {
                hasMore = i < 255;
                nextStart = (short)(i + 1);
                break;
            }
        }

        packets.Add(EncodeRepeat(24, hasMore ? (byte)1 : (byte)2, hasMore ? nextStart : (short)1));
        return Flatten(packets);
    }

    public byte[] BuildItemChunk(short startIndex)
    {
        var packets = new List<byte[]>();
        short nextStart = 1;
        var hasMore = false;

        for (short i = startIndex; i <= 1000; i++)
        {
            AppendIfPresent(packets, EncodeObject(i));
            AppendIfPresent(packets, EncodeMonster(i));

            if (EstimatePayloadLength(packets) >= 1024)
            {
                hasMore = i < 1000;
                nextStart = (short)(i + 1);
                break;
            }
        }

        packets.Add(hasMore ? EncodeRepeat(24, 2, nextStart) : EncodeRepeat(23));
        return Flatten(packets);
    }

    private static int EstimatePayloadLength(IEnumerable<byte[]> packets)
    {
        return packets.Sum(packet => packet.Length);
    }

    private static void AppendIfPresent(List<byte[]> packets, byte[]? packet)
    {
        if (packet is not null)
        {
            packets.Add(packet);
        }
    }

    private byte[]? EncodeNpc(short id)
    {
        var npc = content.Npcs.FirstOrDefault(item => item.Id == id);
        if (npc is null || string.IsNullOrWhiteSpace(npc.Name))
        {
            return null;
        }

        return EncodeSegment(
            85,
            BuildPayload(writer => writer
                .WriteByte((byte)id)
                .WriteByte(npc.Flags)
                .WriteByte(npc.Portrait)
                .WriteByte(npc.Sprite)
                .WriteByte(npc.Direction)
                .WriteString(LegacyEncoding.Cryp(npc.Name))));
    }

    private byte[]? EncodeHall(short id)
    {
        var hall = content.Halls.FirstOrDefault(item => item.Id == id);
        if (hall is null || string.IsNullOrWhiteSpace(hall.Name))
        {
            return null;
        }

        return EncodeSegment(
            82,
            BuildPayload(writer => writer
                .WriteByte((byte)id)
                .WriteString(LegacyEncoding.Cryp(hall.Name))));
    }

    private byte[]? EncodeGuild(short id)
    {
        var guild = content.Guilds.FirstOrDefault(item => item.Id == id);
        if (guild is null || string.IsNullOrWhiteSpace(guild.Name))
        {
            return null;
        }

        var segments = new List<byte[]>
        {
            EncodeSegment(
                70,
                BuildPayload(writer => writer
                    .WriteByte((byte)id)
                    .WriteString(LegacyEncoding.Cryp(guild.Name)))),
            EncodeSegment(
                136,
                BuildPayload(writer => writer
                    .WriteByte((byte)id)
                    .WriteByte(guild.MemberCount)
                    .WriteByte(0)
                    .WriteInt32(guild.AverageRenown)
                    .WriteByte(guild.Symbol1)
                    .WriteByte(guild.Symbol2)
                    .WriteByte(guild.Symbol3)
                    .WriteByte(guild.Hall)))
        };

        return Flatten(segments);
    }

    private byte[]? EncodePrefix(short id)
    {
        var prefix = content.Prefixes.FirstOrDefault(item => item.Id == id);
        if (prefix is null || string.IsNullOrWhiteSpace(prefix.Name))
        {
            return null;
        }

        return EncodeSegment(
            108,
            BuildPayload(writer => writer
                .WriteByte((byte)id)
                .WriteByte(prefix.LightIntensity)
                .WriteByte(prefix.LightRadius)
                .WriteByte(prefix.ModType)
                .WriteByte(prefix.Flags)
                .WriteString(LegacyEncoding.Cryp(prefix.Name))));
    }

    private byte[]? EncodeLight(short id)
    {
        var light = content.Lights.FirstOrDefault(item => item.Id == id);
        if (light is null || string.IsNullOrWhiteSpace(light.Name))
        {
            return null;
        }

        return EncodeSegment(
            129,
            BuildPayload(writer => writer
                .WriteByte((byte)id)
                .WriteByte(light.Red)
                .WriteByte(light.Green)
                .WriteByte(light.Blue)
                .WriteByte(light.Intensity)
                .WriteByte(light.Radius)
                .WriteByte(light.MaxFlicker)
                .WriteByte(light.FlickerRate)
                .WriteString(LegacyEncoding.Cryp(light.Name))));
    }

    private byte[]? EncodeObject(short id)
    {
        var item = content.Objects.FirstOrDefault(objectDefinition => objectDefinition.Id == id);
        if (item is null || item.Picture == 0)
        {
            return null;
        }

        return EncodeSegment(
            31,
            BuildPayload(writer => writer
                .WriteInt16(id)
                .WriteByte(item.Picture)
                .WriteByte(item.Type)
                .WriteBytes(item.Data)
                .WriteByte(item.MinLevel)
                .WriteByte(item.Flags)
                .WriteInt16(item.ClassMask)
                .WriteByte(item.EquipmentPicture)
                .WriteString(LegacyEncoding.Cryp(item.Name))
                .WriteByte(0)
                .WriteString(LegacyEncoding.Cryp(item.Description))));
    }

    private byte[]? EncodeMonster(short id)
    {
        var monster = content.Monsters.FirstOrDefault(item => item.Id == id);
        if (monster is null || monster.Sprite == 0)
        {
            return null;
        }

        return EncodeSegment(
            32,
            BuildPayload(writer => writer
                .WriteInt16(id)
                .WriteByte(monster.Sprite)
                .WriteInt16(monster.Hp)
                .WriteByte(monster.Flags)
                .WriteByte(monster.DeathSound)
                .WriteByte(monster.AttackSound)
                .WriteByte(monster.Alpha)
                .WriteByte(monster.Red)
                .WriteByte(monster.Green)
                .WriteByte(monster.Blue)
                .WriteByte(monster.Light)
                .WriteByte(monster.Flags2)
                .WriteString(LegacyEncoding.Cryp(monster.Name))));
    }

    private static byte[] EncodeRepeat(byte packetId)
    {
        return EncodeSegment(35, new[] { packetId });
    }

    private static byte[] EncodeRepeat(byte packetId, byte pageType, short startIndex)
    {
        return EncodeSegment(
            35,
            BuildPayload(writer => writer
                .WriteByte(packetId)
                .WriteByte(pageType)
                .WriteInt16(startIndex)));
    }

    private static byte[] EncodeSegment(byte packetId, byte[] payload)
    {
        var body = new byte[1 + payload.Length];
        body[0] = packetId;
        payload.CopyTo(body, 1);

        var segment = new byte[2 + body.Length];
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(0, 2), unchecked((ushort)body.Length));
        body.CopyTo(segment, 2);
        return segment;
    }

    private static byte[] BuildPayload(Action<LegacyPacketWriter> build)
    {
        var writer = new LegacyPacketWriter();
        build(writer);
        return writer.ToArray();
    }

    private static byte[] Flatten(IEnumerable<byte[]> packets)
    {
        using var stream = new MemoryStream();
        foreach (var packet in packets)
        {
            stream.Write(packet);
        }

        return stream.ToArray();
    }
}
