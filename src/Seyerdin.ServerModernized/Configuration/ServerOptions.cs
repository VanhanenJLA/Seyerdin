namespace Seyerdin.ServerModernized.Configuration;

public sealed class ServerOptions
{
    public int Port { get; init; } = 3017;

    public string ServerName { get; init; } = "Seyerdin";

    public int CurrentClientVersion { get; init; } = 58;

    public int MaxUsers { get; init; } = 80;

    public string DownloadSite { get; init; } = "http://www.Seyerdin.com";

    public string Motd { get; init; } = string.Empty;

    public string AccountsFilePath { get; init; } = Path.Combine("data", "modernized", "accounts.json");

    public string ClassesFilePath { get; init; } = Path.Combine("Server", "classes.ini");

    public string MapsDirectoryPath { get; init; } = Path.Combine("data", "modernized", "maps");

    public string ContentFilePath { get; init; } = Path.Combine("data", "modernized", "content.json");
}
