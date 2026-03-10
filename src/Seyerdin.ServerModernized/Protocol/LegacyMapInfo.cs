namespace Seyerdin.ServerModernized.Protocol;

public sealed record LegacyMapInfo(
    string Name,
    int Version,
    int Checksum,
    short ExitUp,
    short ExitDown,
    short ExitLeft,
    short ExitRight,
    short BootMap,
    byte BootX,
    byte BootY,
    byte Intensity);
