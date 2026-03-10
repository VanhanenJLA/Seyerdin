using System.Buffers.Binary;
using Seyerdin.ServerModernized.Domain;

namespace Seyerdin.ServerModernized.Protocol;

public sealed class LegacyPacketWriter
{
    private readonly MemoryStream stream = new();

    public LegacyPacketWriter WriteByte(byte value)
    {
        stream.WriteByte(value);
        return this;
    }

    public LegacyPacketWriter WriteInt16(short value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, unchecked((ushort)value));
        stream.Write(buffer);
        return this;
    }

    public LegacyPacketWriter WriteInt32(int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, unchecked((uint)value));
        stream.Write(buffer);
        return this;
    }

    public LegacyPacketWriter WriteBytes(ReadOnlySpan<byte> bytes)
    {
        stream.Write(bytes);
        return this;
    }

    public LegacyPacketWriter WriteString(string value)
    {
        return WriteBytes(LegacyEncoding.GetBytes(value));
    }

    public byte[] ToArray() => stream.ToArray();

    public static byte[] BuildCharacterDataPayload(CharacterRecord? character, byte access, byte playerIndex)
    {
        if (character is null || character.Level == 0 || character.ClassId == 0)
        {
            return new[] { (byte)3 };
        }

        var writer = new LegacyPacketWriter()
            .WriteByte(3)
            .WriteByte(character.Level)
            .WriteByte(character.ClassId)
            .WriteByte(0)
            .WriteByte(0)
            .WriteByte(0)
            .WriteByte(0)
            .WriteByte(character.Gender)
            .WriteByte(character.Sprite)
            .WriteInt16(character.Hp)
            .WriteInt16(character.Energy)
            .WriteInt16(character.Mana)
            .WriteInt16(character.MaxHp)
            .WriteInt16(character.MaxEnergy)
            .WriteInt16(character.MaxMana)
            .WriteByte(character.Strength)
            .WriteByte(character.Agility)
            .WriteByte(character.Endurance)
            .WriteByte(character.Wisdom)
            .WriteByte(character.Constitution)
            .WriteByte(character.Intelligence)
            .WriteByte(character.Level)
            .WriteByte(character.Status)
            .WriteByte(character.GuildId)
            .WriteByte(character.GuildRank)
            .WriteByte(access)
            .WriteByte(playerIndex)
            .WriteInt32(character.Experience)
            .WriteByte(character.Squelched)
            .WriteInt32(character.StatusEffect)
            .WriteInt16(character.StatPoints)
            .WriteInt16(character.SkillPoints)
            .WriteBytes(new byte[255])
            .WriteBytes(new byte[1020])
            .WriteString(character.Name)
            .WriteByte(0)
            .WriteByte(0)
            .WriteString(character.GuildName);

        return writer.ToArray();
    }
}
