using System.Globalization;

namespace Seyerdin.ServerModernized.Tooling;

public static class ToolCommandParser
{
    public static ToolCommandOptions Parse(IReadOnlyList<string> args)
    {
        short? seedShellMapId = null;

        for (var i = 0; i < args.Count; i++)
        {
            if (args[i] == "--seed-shell-map" && i + 1 < args.Count)
            {
                if (short.TryParse(args[++i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mapId))
                {
                    seedShellMapId = mapId;
                }
            }
        }

        return new ToolCommandOptions
        {
            SeedShellMapId = seedShellMapId,
        };
    }
}
