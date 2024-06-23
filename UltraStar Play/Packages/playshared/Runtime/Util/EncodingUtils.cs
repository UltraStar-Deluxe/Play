using System;
using System.Collections.Generic;
using System.Text;

public static class EncodingUtils
{
    public static readonly Encoding utf8Bom = Encoding.UTF8;
    public static readonly Encoding utf8NoBom = new UTF8Encoding(false);

    public static Encoding GetUtf8Encoding(bool writeByteOrderMark)
    {
        return writeByteOrderMark
            ? utf8Bom
            : utf8NoBom;
    }

    public static Encoding GetEncoding(string name)
    {
        if (TryGetEncoding(name, out Encoding encoding))
        {
            return encoding;
        }

        string normalizedName = NormalizeEncodingName(name);
        return Encoding.GetEncoding(normalizedName);
    }

    private static string NormalizeEncodingName(string name)
    {
        Dictionary<string, string> nameToNormalizedName = new()
        {
            { "utf8", "utf-8" },
            { "utf16", "utf-16" },
            { "utf32", "utf-32" },
            { "ansi", "windows-1252" },
            { "1252", "windows-1252" },
            { "cp1252", "windows-1252" },
            { "cp-1252", "windows-1252" },
            { "windows1252", "windows-1252" },
        };

        foreach (KeyValuePair<string, string> entry in nameToNormalizedName)
        {
            if (string.Equals(name, entry.Key, StringComparison.InvariantCultureIgnoreCase))
            {
                return entry.Value;
            }
        }

        return name;
    }

    private static bool TryGetEncoding(string name, out Encoding encoding)
    {
        Dictionary<string, Encoding> nameToEncoding = new()
        {
            { "utf32", Encoding.UTF32 },
            { "utf-32", Encoding.UTF32 },
        };

        foreach (KeyValuePair<string, Encoding> entry in nameToEncoding)
        {
            if (string.Equals(name, entry.Key, StringComparison.InvariantCultureIgnoreCase))
            {
                encoding = entry.Value;
                return true;
            }
        }

        encoding = Encoding.UTF8;
        return false;
    }
}
