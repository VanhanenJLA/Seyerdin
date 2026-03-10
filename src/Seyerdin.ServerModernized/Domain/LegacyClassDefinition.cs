namespace Seyerdin.ServerModernized.Domain;

public sealed class LegacyClassDefinition
{
    public byte Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public short StartHp { get; init; }

    public short StartEnergy { get; init; }

    public short StartMana { get; init; }

    public byte StartStrength { get; init; }

    public byte StartAgility { get; init; }

    public byte StartEndurance { get; init; }

    public byte StartWisdom { get; init; }

    public byte StartConstitution { get; init; }

    public byte StartIntelligence { get; init; }

    public bool Enabled { get; init; }
}
