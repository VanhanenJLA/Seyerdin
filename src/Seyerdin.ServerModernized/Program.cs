using Seyerdin.ServerModernized.Configuration;
using Seyerdin.ServerModernized.Hosting;
using Seyerdin.ServerModernized.Tooling;

return await ProgramEntry.RunAsync(args);

internal static class ProgramEntry
{
    public static async Task<int> RunAsync(string[] args)
    {
        var options = ServerOptionsLoader.Load(args);
        var toolOptions = ToolCommandParser.Parse(args);

        if (toolOptions.SeedShellMapId is { } seedMapId)
        {
            return LegacyMapSeedCommand.Run(options.MapsDirectoryPath, seedMapId);
        }

        if (!string.IsNullOrWhiteSpace(toolOptions.InspectMapPath))
        {
            return LegacyMapInspectCommand.Run(toolOptions.InspectMapPath);
        }

        if (!string.IsNullOrWhiteSpace(toolOptions.ImportMapPath))
        {
            return LegacyMapImportCommand.Run(
                options.MapsDirectoryPath,
                toolOptions.ImportMapPath,
                toolOptions.ImportMapId);
        }

        if (!string.IsNullOrWhiteSpace(toolOptions.ImportBundlePath))
        {
            return LegacyBundleImportCommand.Run(
                toolOptions.ImportBundlePath,
                options.ContentFilePath,
                options.MapsDirectoryPath);
        }

        var server = new LegacyCompatibilityServer(options);

        Console.WriteLine(
            $"Starting {options.ServerName} compatibility server on port {options.Port} " +
            $"for legacy client version {options.CurrentClientVersion}.");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        await server.RunAsync(cts.Token);
        return 0;
    }
}
