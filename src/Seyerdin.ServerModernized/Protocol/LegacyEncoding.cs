using System.Text;

namespace Seyerdin.ServerModernized.Protocol;

public static class LegacyEncoding
{
    public static byte[] GetBytes(string value)
    {
        return Encoding.Latin1.GetBytes(value);
    }

    public static string GetString(ReadOnlySpan<byte> bytes)
    {
        return Encoding.Latin1.GetString(bytes);
    }

    public static string Cryp(string value)
    {
        var bytes = GetBytes(value);
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(219 ^ bytes[i]);
        }

        return GetString(bytes);
    }

    public static bool IsValidName(string value)
    {
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '_')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
