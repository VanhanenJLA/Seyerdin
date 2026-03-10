using System.Globalization;
using Seyerdin.ServerModernized.Domain;

namespace Seyerdin.ServerModernized.Infrastructure;

public sealed class LegacyClassCatalog
{
    private readonly IReadOnlyDictionary<byte, LegacyClassDefinition> classes;

    private LegacyClassCatalog(IReadOnlyDictionary<byte, LegacyClassDefinition> classes)
    {
        this.classes = classes;
    }

    public static LegacyClassCatalog Load(string path)
    {
        var rawSections = IniSectionReader.Read(path);
        var classes = new Dictionary<byte, LegacyClassDefinition>();

        for (byte classId = 1; classId <= 10; classId++)
        {
            var sectionName = $"CLASS{classId}";
            if (!rawSections.TryGetValue(sectionName, out var section))
            {
                continue;
            }

            classes[classId] = new LegacyClassDefinition
            {
                Id = classId,
                Name = GetString(section, "Name", $"Class {classId}"),
                StartHp = (short)GetInt(section, "StartHP", 30),
                StartEnergy = (short)GetInt(section, "StartEnergy", 75),
                StartMana = (short)GetInt(section, "StartMana", 40),
                StartStrength = (byte)GetInt(section, "StartStrength", 0),
                StartAgility = (byte)GetInt(section, "StartAgility", 0),
                StartEndurance = (byte)GetInt(section, "StartEndurance", 0),
                StartWisdom = (byte)GetInt(section, "StartWisdom", 0),
                StartConstitution = (byte)GetInt(section, "StartConstitution", 0),
                StartIntelligence = (byte)GetInt(section, "StartIntelligence", 0),
                Enabled = GetInt(section, "Enabled", 0) == 1,
            };
        }

        return new LegacyClassCatalog(classes);
    }

    public LegacyClassDefinition ResolveOrDefault(byte requestedClassId)
    {
        if (classes.TryGetValue(requestedClassId, out var requested) && requested.Enabled)
        {
            return requested;
        }

        var fallback = classes.Values
            .OrderBy(definition => definition.Id)
            .FirstOrDefault(definition => definition.Enabled);

        if (fallback is null)
        {
            throw new InvalidOperationException("No enabled legacy classes were found.");
        }

        return fallback;
    }

    private static int GetInt(IReadOnlyDictionary<string, string> section, string key, int fallback)
    {
        return section.TryGetValue(key, out var value) &&
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static string GetString(IReadOnlyDictionary<string, string> section, string key, string fallback)
    {
        return section.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }
}
