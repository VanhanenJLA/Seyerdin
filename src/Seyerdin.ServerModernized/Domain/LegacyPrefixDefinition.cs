namespace Seyerdin.ServerModernized.Domain;

public sealed class LegacyPrefixDefinition
{
    public byte Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public byte LightIntensity { get; init; }

    public byte LightRadius { get; init; }

    public byte ModType { get; init; }

    public byte Flags { get; init; }
}
