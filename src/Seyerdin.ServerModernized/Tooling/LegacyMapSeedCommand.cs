using Seyerdin.ServerModernized.Infrastructure;
using Seyerdin.ServerModernized.Protocol;

namespace Seyerdin.ServerModernized.Tooling;

public static class LegacyMapSeedCommand
{
    public static int Run(string mapsDirectoryPath, short mapId)
    {
        var store = new FileSystemLegacyMapStore(mapsDirectoryPath);
        store.Save(mapId, LegacyWorldPacketFactory.CreateShellMapData());

        Console.WriteLine($"Wrote shell map {mapId} to {LegacyMapStorePaths.GetPrimaryPath(mapsDirectoryPath, mapId)}");
        return 0;
    }
}
