namespace Seyerdin.ServerModernized.Domain;

public sealed class CharacterRecord
{
    public string Name { get; set; } = string.Empty;

    public byte Level { get; set; }

    public byte ClassId { get; set; }

    public byte Gender { get; set; }

    public byte Sprite { get; set; }

    public short Hp { get; set; }

    public short Energy { get; set; }

    public short Mana { get; set; }

    public short MaxHp { get; set; }

    public short MaxEnergy { get; set; }

    public short MaxMana { get; set; }

    public byte Strength { get; set; }

    public byte Agility { get; set; }

    public byte Endurance { get; set; }

    public byte Wisdom { get; set; }

    public byte Constitution { get; set; }

    public byte Intelligence { get; set; }

    public byte Status { get; set; }

    public byte GuildId { get; set; }

    public byte GuildRank { get; set; }

    public int Experience { get; set; }

    public byte Squelched { get; set; }

    public int StatusEffect { get; set; }

    public short StatPoints { get; set; }

    public short SkillPoints { get; set; }

    public string GuildName { get; set; } = string.Empty;
}
