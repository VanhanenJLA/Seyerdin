using System.Globalization;

namespace Seyerdin.ServerModernized.Tooling;

public static class ToolCommandParser
{
    public static ToolCommandOptions Parse(IReadOnlyList<string> args)
    {
        short? seedShellMapId = null;
        string? inspectMapPath = null;
        string? importMapPath = null;
        short? importMapId = null;
        string? importBundlePath = null;

        for (var i = 0; i < args.Count; i++)
        {
            if (args[i] == "--seed-shell-map" && i + 1 < args.Count)
            {
                if (short.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mapId))
                {
                    seedShellMapId = mapId;
                }
            }
            else if (args[i] == "--inspect-map" && i + 1 < args.Count)
            {
                inspectMapPath = args[++i];
            }
            else if (args[i] == "--import-map" && i + 1 < args.Count)
            {
                importMapPath = args[++i];
            }
            else if (args[i] == "--map-id" && i + 1 < args.Count)
            {
                if (short.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mapId))
                {
                    importMapId = mapId;
                }
            }
            else if (args[i] == "--import-bundle" && i + 1 < args.Count)
            {
                importBundlePath = args[++i];
            }
        }

        return new ToolCommandOptions
        {
            SeedShellMapId = seedShellMapId,
            InspectMapPath = inspectMapPath,
            ImportMapPath = importMapPath,
            ImportMapId = importMapId,
            ImportBundlePath = importBundlePath,
        };
    }
}
