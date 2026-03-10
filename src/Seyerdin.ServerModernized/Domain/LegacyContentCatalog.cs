namespace Seyerdin.ServerModernized.Domain;

public sealed class LegacyContentCatalog
{
    public List<LegacyObjectDefinition> Objects { get; init; } = [];

    public List<LegacyMonsterDefinition> Monsters { get; init; } = [];

    public List<LegacyNpcDefinition> Npcs { get; init; } = [];

    public List<LegacyHallDefinition> Halls { get; init; } = [];

    public List<LegacyGuildDefinition> Guilds { get; init; } = [];

    public List<LegacyPrefixDefinition> Prefixes { get; init; } = [];

    public List<LegacyLightDefinition> Lights { get; init; } = [];
}
