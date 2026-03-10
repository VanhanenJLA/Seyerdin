namespace Seyerdin.ServerModernized.Domain;

public sealed class LegacyLightDefinition
{
    public byte Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public byte Red { get; init; }

    public byte Green { get; init; }

    public byte Blue { get; init; }

    public byte Intensity { get; init; }

    public byte Radius { get; init; }

    public byte MaxFlicker { get; init; }

    public byte FlickerRate { get; init; }
}
