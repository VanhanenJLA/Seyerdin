using Seyerdin.ServerModernized.Protocol;

namespace Seyerdin.ServerModernized.Tooling;

public static class LegacyMapInspectCommand
{
    public static int Run(string path)
    {
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Map file not found: {path}");
            return 1;
        }

        var data = File.ReadAllBytes(path);
        if (data.Length != LegacyWorldPacketFactory.LegacyMapLength)
        {
            Console.Error.WriteLine(
                $"Invalid map length {data.Length}. Expected {LegacyWorldPacketFactory.LegacyMapLength} bytes.");
            return 1;
        }

        var map = LegacyMapParser.Parse(data);
        Console.WriteLine($"Name: {map.Name}");
        Console.WriteLine($"Version: {map.Version}");
        Console.WriteLine($"Checksum: {map.Checksum}");
        Console.WriteLine($"Exits: up={map.ExitUp} down={map.ExitDown} left={map.ExitLeft} right={map.ExitRight}");
        Console.WriteLine($"Boot: map={map.BootMap} x={map.BootX} y={map.BootY}");
        Console.WriteLine($"Intensity: {map.Intensity}");
        return 0;
    }
}
