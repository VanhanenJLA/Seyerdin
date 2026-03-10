namespace Seyerdin.ServerModernized.Domain;

public sealed class LegacyMonsterDefinition
{
    public short Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public byte Sprite { get; init; }

    public short Hp { get; init; }

    public byte Flags { get; init; }

    public byte DeathSound { get; init; }

    public byte AttackSound { get; init; }

    public byte Alpha { get; init; }

    public byte Red { get; init; }

    public byte Green { get; init; }

    public byte Blue { get; init; }

    public byte Light { get; init; }

    public byte Flags2 { get; init; }
}
