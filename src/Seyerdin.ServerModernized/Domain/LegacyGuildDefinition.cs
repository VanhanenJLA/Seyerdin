namespace Seyerdin.ServerModernized.Domain;

public sealed class LegacyGuildDefinition
{
    public byte Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public byte Symbol1 { get; init; }

    public byte Symbol2 { get; init; }

    public byte Symbol3 { get; init; }

    public byte Hall { get; init; }

    public int AverageRenown { get; init; }

    public byte MemberCount { get; init; }
}
