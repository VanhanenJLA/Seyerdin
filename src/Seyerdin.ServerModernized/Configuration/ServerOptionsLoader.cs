using System.Globalization;

namespace Seyerdin.ServerModernized.Configuration;

public static class ServerOptionsLoader
{
    private const string LegacyIniPath = "Server/Server.ini";

    public static ServerOptions Load()
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

        return new ServerOptions
        {
            Port = GetInt(values, "Port", 3017),
            ServerName = GetString(values, "Name", "Seyerdin"),
            MaxUsers = GetInt(values, "MaxUsers", 80),
            CurrentClientVersion = 58,
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
}
