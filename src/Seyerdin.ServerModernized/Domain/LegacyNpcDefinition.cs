namespace Seyerdin.ServerModernized.Domain;

public sealed class LegacyNpcDefinition
{
    public byte Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public byte Flags { get; init; }

    public byte Portrait { get; init; }

    public byte Sprite { get; init; }

    public byte Direction { get; init; }
}
