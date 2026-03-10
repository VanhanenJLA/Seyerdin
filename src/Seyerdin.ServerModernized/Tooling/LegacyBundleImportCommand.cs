using System.Globalization;
using System.Text.Json;
using Seyerdin.ServerModernized.Domain;
using Seyerdin.ServerModernized.Infrastructure;
using Seyerdin.ServerModernized.Protocol;

namespace Seyerdin.ServerModernized.Tooling;

public static class LegacyBundleImportCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static int Run(string bundleDirectoryPath, string contentFilePath, string mapsDirectoryPath)
    {
        if (!Directory.Exists(bundleDirectoryPath))
        {
            Console.Error.WriteLine($"Bundle directory not found: {bundleDirectoryPath}");
            return 1;
        }

        var catalog = new LegacyContentCatalog
        {
            Objects = LoadObjects(bundleDirectoryPath),
            Monsters = LoadMonsters(bundleDirectoryPath),
            Npcs = LoadNpcs(bundleDirectoryPath),
            Halls = LoadHalls(bundleDirectoryPath),
            Guilds = LoadGuilds(bundleDirectoryPath),
            Prefixes = LoadPrefixes(bundleDirectoryPath),
            Lights = LoadLights(bundleDirectoryPath),
        };

        var contentDirectory = Path.GetDirectoryName(contentFilePath);
        if (!string.IsNullOrWhiteSpace(contentDirectory))
        {
            Directory.CreateDirectory(contentDirectory);
        }

        File.WriteAllText(contentFilePath, JsonSerializer.Serialize(catalog, JsonOptions));
        ImportMaps(bundleDirectoryPath, mapsDirectoryPath);

        Console.WriteLine($"Wrote content catalog to {contentFilePath}");
        Console.WriteLine($"Imported maps into {mapsDirectoryPath}");
        return 0;
    }

    private static List<LegacyObjectDefinition> LoadObjects(string bundleDirectoryPath)
    {
        return ReadRows(bundleDirectoryPath, "Objects.csv")
            .Select(row => new LegacyObjectDefinition
            {
                Id = GetInt16(row, "Number"),
                Name = GetString(row, "Name"),
                Description = GetString(row, "Description"),
                Picture = GetByte(row, "Picture"),
                Type = GetByte(row, "Type"),
                Data = Enumerable.Range(1, 10).Select(index => GetByte(row, $"Data{index}")).ToArray(),
                Flags = GetByte(row, "Flags"),
                ClassMask = GetInt16(row, "Class"),
                MinLevel = GetByte(row, "MinLevel"),
                EquipmentPicture = GetByte(row, "EquipmentPicture"),
            })
            .Where(item => item.Id > 0)
            .ToList();
    }

    private static List<LegacyMonsterDefinition> LoadMonsters(string bundleDirectoryPath)
    {
        return ReadRows(bundleDirectoryPath, "Monsters.csv")
            .Select(row => new LegacyMonsterDefinition
            {
                Id = GetInt16(row, "Number"),
                Name = GetString(row, "Name"),
                Sprite = GetByte(row, "Sprite"),
                Hp = GetInt16(row, "HP"),
                Flags = GetByte(row, "Flags"),
                DeathSound = GetByte(row, "DeathSound"),
                AttackSound = GetByte(row, "AttackSound"),
                Alpha = GetByte(row, "Alpha"),
                Red = GetByte(row, "Red"),
                Green = GetByte(row, "Green"),
                Blue = GetByte(row, "Blue"),
                Light = GetByte(row, "Light"),
                Flags2 = GetByte(row, "Flags2"),
            })
            .Where(item => item.Id > 0)
            .ToList();
    }

    private static List<LegacyNpcDefinition> LoadNpcs(string bundleDirectoryPath)
    {
        return ReadRows(bundleDirectoryPath, "NPCs.csv")
            .Select(row => new LegacyNpcDefinition
            {
                Id = GetByte(row, "Number"),
                Name = GetString(row, "Name"),
                Flags = GetByte(row, "Flags"),
                Portrait = GetByte(row, "Portrait"),
                Sprite = GetByte(row, "Sprite"),
                Direction = GetByte(row, "Direction"),
            })
            .Where(item => item.Id > 0)
            .ToList();
    }

    private static List<LegacyHallDefinition> LoadHalls(string bundleDirectoryPath)
    {
        return ReadRows(bundleDirectoryPath, "Halls.csv")
            .Select(row => new LegacyHallDefinition
            {
                Id = GetByte(row, "Number"),
                Name = GetString(row, "Name"),
            })
            .Where(item => item.Id > 0)
            .ToList();
    }

    private static List<LegacyGuildDefinition> LoadGuilds(string bundleDirectoryPath)
    {
        return ReadRows(bundleDirectoryPath, "Guilds.csv")
            .Select(row => new LegacyGuildDefinition
            {
                Id = GetByte(row, "Number"),
                Name = GetString(row, "Name"),
                Symbol1 = GetByte(row, "Symbol"),
                Symbol2 = ClampToByte(GetInt32(row, "GuildKills")),
                Symbol3 = ClampToByte(GetInt32(row, "GuildDeaths")),
                Hall = GetByte(row, "Hall"),
                AverageRenown = 0,
                MemberCount = CountGuildMembers(row),
            })
            .Where(item => item.Id > 0)
            .ToList();
    }

    private static List<LegacyPrefixDefinition> LoadPrefixes(string bundleDirectoryPath)
    {
        return ReadRows(bundleDirectoryPath, "Prefix.csv")
            .Select(row =>
            {
                var packed = GetBytes(row, "Data");
                return new LegacyPrefixDefinition
                {
                    Id = GetByte(row, "Number"),
                    Name = GetString(row, "Name"),
                    ModType = packed.ElementAtOrDefault(0),
                    Flags = packed.ElementAtOrDefault(3),
                    LightIntensity = packed.ElementAtOrDefault(8),
                    LightRadius = packed.ElementAtOrDefault(9),
                };
            })
            .Where(item => item.Id > 0)
            .ToList();
    }

    private static List<LegacyLightDefinition> LoadLights(string bundleDirectoryPath)
    {
        return ReadRows(bundleDirectoryPath, "Lights.csv")
            .Select(row =>
            {
                var packed = GetBytes(row, "Data");
                return new LegacyLightDefinition
                {
                    Id = GetByte(row, "Number"),
                    Name = GetString(row, "Name"),
                    Red = packed.ElementAtOrDefault(0),
                    Green = packed.ElementAtOrDefault(1),
                    Blue = packed.ElementAtOrDefault(2),
                    Intensity = packed.ElementAtOrDefault(3),
                    Radius = packed.ElementAtOrDefault(4),
                    MaxFlicker = packed.ElementAtOrDefault(5),
                    FlickerRate = packed.ElementAtOrDefault(6),
                };
            })
            .Where(item => item.Id > 0)
            .ToList();
    }

    private static void ImportMaps(string bundleDirectoryPath, string mapsDirectoryPath)
    {
        var sourceDirectory = Path.Combine(bundleDirectoryPath, "maps");
        if (!Directory.Exists(sourceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(mapsDirectoryPath);
        foreach (var path in Directory.EnumerateFiles(sourceDirectory))
        {
            var fileName = Path.GetFileName(path);
            File.Copy(path, Path.Combine(mapsDirectoryPath, fileName), true);
        }
    }

    private static List<Dictionary<string, string>> ReadRows(string bundleDirectoryPath, string fileName)
    {
        var path = Path.Combine(bundleDirectoryPath, fileName);
        return File.Exists(path) ? SimpleCsvReader.Read(path) : [];
    }

    private static string GetString(IReadOnlyDictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out var value) ? value : string.Empty;
    }

    private static byte GetByte(IReadOnlyDictionary<string, string> row, string key)
    {
        return ClampToByte(GetInt32(row, key));
    }

    private static short GetInt16(IReadOnlyDictionary<string, string> row, string key)
    {
        return short.TryParse(GetString(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : (short)0;
    }

    private static int GetInt32(IReadOnlyDictionary<string, string> row, string key)
    {
        return int.TryParse(GetString(row, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }

    private static byte ClampToByte(int value)
    {
        return (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);
    }

    private static byte[] GetBytes(IReadOnlyDictionary<string, string> row, string key)
    {
        return LegacyEncoding.GetBytes(GetString(row, key));
    }

    private static byte CountGuildMembers(IReadOnlyDictionary<string, string> row)
    {
        var count = 0;
        for (var i = 0; i < 20; i++)
        {
            if (!string.IsNullOrWhiteSpace(GetString(row, $"MemberName{i}")))
            {
                count++;
            }
        }

        return ClampToByte(count);
    }
}
