namespace Seyerdin.ServerModernized.Protocol;

public sealed record LegacyMapDescriptor(byte[] Data, int Version, int Checksum);
