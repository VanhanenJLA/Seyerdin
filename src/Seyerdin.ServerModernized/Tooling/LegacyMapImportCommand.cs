using System.Globalization;
using Seyerdin.ServerModernized.Infrastructure;
using Seyerdin.ServerModernized.Protocol;

namespace Seyerdin.ServerModernized.Tooling;

public static class LegacyMapImportCommand
{
    public static int Run(string mapsDirectoryPath, string sourcePath, short? forcedMapId = null)
    {
        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Map file not found: {sourcePath}");
            return 1;
        }

        var data = File.ReadAllBytes(sourcePath);
        if (data.Length != LegacyWorldPacketFactory.LegacyMapLength)
        {
            Console.Error.WriteLine(
                $"Invalid map length {data.Length}. Expected {LegacyWorldPacketFactory.LegacyMapLength} bytes.");
            return 1;
        }

        var mapId = forcedMapId ?? ParseMapIdFromFileName(sourcePath);
        if (mapId is null or <= 0)
        {
            Console.Error.WriteLine("Map id was not provided and could not be inferred from the file name.");
            return 1;
        }

        var map = LegacyMapParser.Parse(data);
        var store = new FileSystemLegacyMapStore(mapsDirectoryPath);
        store.Save(mapId.Value, data);

        Console.WriteLine(
            $"Imported map {mapId.Value} ({map.Name}, version {map.Version}, checksum {map.Checksum}) " +
            $"to {LegacyMapStorePaths.GetPrimaryPath(mapsDirectoryPath, mapId.Value)}");
        return 0;
    }

    private static short? ParseMapIdFromFileName(string sourcePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourcePath);
        return short.TryParse(fileName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mapId)
            ? mapId
            : null;
    }
}
