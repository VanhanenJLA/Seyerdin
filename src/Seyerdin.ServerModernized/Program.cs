using Seyerdin.ServerModernized.Configuration;
using Seyerdin.ServerModernized.Hosting;

var options = ServerOptionsLoader.Load();
var server = new LegacyCompatibilityServer(options);

Console.WriteLine(
    $"Starting {options.ServerName} compatibility server on port {options.Port} " +
    $"for legacy client version {options.CurrentClientVersion}.");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, args) =>
{
    args.Cancel = true;
    cts.Cancel();
};

await server.RunAsync(cts.Token);
