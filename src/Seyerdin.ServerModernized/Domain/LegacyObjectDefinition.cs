namespace Seyerdin.ServerModernized.Domain;

public sealed class LegacyObjectDefinition
{
    public short Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public byte Picture { get; init; }

    public byte Type { get; init; }

    public byte[] Data { get; init; } = new byte[10];

    public byte Flags { get; init; }

    public short ClassMask { get; init; }

    public byte MinLevel { get; init; }

    public byte EquipmentPicture { get; init; }
}
