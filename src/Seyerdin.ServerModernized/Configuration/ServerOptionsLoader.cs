using System.Globalization;

namespace Seyerdin.ServerModernized.Configuration;

public static class ServerOptionsLoader
{
    private const string LegacyIniPath = "Server/Server.ini";

    public static ServerOptions Load(string[]? args = null)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (File.Exists(LegacyIniPath))
        {
            foreach (var rawLine in File.ReadAllLines(LegacyIniPath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('[') || line.StartsWith(';'))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();
                values[key] = value;
            }
        }

        var overrides = ParseOverrides(args ?? Array.Empty<string>());

        return new ServerOptions
        {
            Port = GetInt(overrides, "Port", GetInt(values, "Port", 3017)),
            ServerName = GetString(values, "Name", "Seyerdin"),
            MaxUsers = GetInt(values, "MaxUsers", 80),
            CurrentClientVersion = 58,
            DownloadSite = "http://www.Seyerdin.com",
            Motd = GetString(overrides, "Motd", string.Empty),
            AccountsFilePath = GetString(
                overrides,
                "AccountsFilePath",
                Path.Combine("data", "modernized", "accounts.json")),
            MapsDirectoryPath = GetString(
                overrides,
                "MapsDirectoryPath",
                Path.Combine("data", "modernized", "maps")),
            ContentFilePath = GetString(
                overrides,
                "ContentFilePath",
                Path.Combine("data", "modernized", "content.json")),
        };
    }

    private static int GetInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
    {
        return values.TryGetValue(key, out var value) &&
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static string GetString(IReadOnlyDictionary<string, string> values, string key, string fallback)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static Dictionary<string, string> ParseOverrides(IReadOnlyList<string> args)
    {
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--port" when i + 1 < args.Count:
                    overrides["Port"] = args[++i];
                    break;
                case "--accounts" when i + 1 < args.Count:
                    overrides["AccountsFilePath"] = args[++i];
                    break;
                case "--motd" when i + 1 < args.Count:
                    overrides["Motd"] = args[++i];
                    break;
                case "--maps" when i + 1 < args.Count:
                    overrides["MapsDirectoryPath"] = args[++i];
                    break;
                case "--content" when i + 1 < args.Count:
                    overrides["ContentFilePath"] = args[++i];
                    break;
            }
        }

        return overrides;
    }
}
