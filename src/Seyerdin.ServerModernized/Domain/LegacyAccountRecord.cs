namespace Seyerdin.ServerModernized.Domain;

public sealed class LegacyAccountRecord
{
    public string UserName { get; set; } = string.Empty;

    public string PasswordCipherText { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public byte Access { get; set; }

    public CharacterRecord? Character { get; set; }
}
