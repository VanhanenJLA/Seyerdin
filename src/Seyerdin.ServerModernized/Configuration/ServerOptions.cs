namespace Seyerdin.ServerModernized.Configuration;

public sealed class ServerOptions
{
    public int Port { get; init; } = 3017;

    public string ServerName { get; init; } = "Seyerdin";

    public int CurrentClientVersion { get; init; } = 58;

    public int MaxUsers { get; init; } = 80;
}
