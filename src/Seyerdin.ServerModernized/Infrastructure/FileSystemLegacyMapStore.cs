using Seyerdin.ServerModernized.Protocol;

namespace Seyerdin.ServerModernized.Infrastructure;

public sealed class FileSystemLegacyMapStore : ILegacyMapStore
{
    private readonly string mapsDirectoryPath;

    public FileSystemLegacyMapStore(string mapsDirectoryPath)
    {
        this.mapsDirectoryPath = mapsDirectoryPath;
    }

    public byte[] LoadMapOrDefault(short mapId)
    {
        foreach (var path in GetCandidatePaths(mapId))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var bytes = File.ReadAllBytes(path);
            if (bytes.Length == LegacyWorldPacketFactory.LegacyMapLength)
            {
                return bytes;
            }
        }

        return LegacyWorldPacketFactory.CreateShellMapData();
    }

    private IEnumerable<string> GetCandidatePaths(short mapId)
    {
        yield return Path.Combine(mapsDirectoryPath, $"{mapId}.map");
        yield return Path.Combine(mapsDirectoryPath, $"{mapId}.bin");
        yield return Path.Combine(mapsDirectoryPath, $"{mapId:D4}.map");
        yield return Path.Combine(mapsDirectoryPath, $"{mapId:D4}.bin");
    }
}
