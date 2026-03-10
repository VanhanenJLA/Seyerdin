namespace Seyerdin.ServerModernized.Infrastructure;

public static class SimpleCsvReader
{
    public static List<Dictionary<string, string>> Read(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            return [];
        }

        var headers = ParseLine(lines[0]);
        var rows = new List<Dictionary<string, string>>();

        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            var values = ParseLine(lines[i]);
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var column = 0; column < headers.Count; column++)
            {
                row[headers[column]] = column < values.Count ? values[column] : string.Empty;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static List<string> ParseLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values;
    }
}
