using System.Text.Json;
using Seyerdin.ServerModernized.Domain;

namespace Seyerdin.ServerModernized.Infrastructure;

public static class LegacyContentCatalogLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static LegacyContentCatalog Load(string path)
    {
        if (!File.Exists(path))
        {
            return new LegacyContentCatalog();
        }

        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<LegacyContentCatalog>(stream, SerializerOptions)
               ?? new LegacyContentCatalog();
    }
}
