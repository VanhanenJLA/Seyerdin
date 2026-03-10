namespace Seyerdin.ServerModernized.Infrastructure;

public static class LegacyMapStorePaths
{
    public static string GetPrimaryPath(string mapsDirectoryPath, short mapId)
    {
        return Path.Combine(mapsDirectoryPath, $"{mapId}.map");
    }
}
