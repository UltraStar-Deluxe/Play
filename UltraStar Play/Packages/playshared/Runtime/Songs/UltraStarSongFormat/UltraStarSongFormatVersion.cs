using System;
using System.Linq;

public struct UltraStarSongFormatVersion
{
    public static readonly UltraStarSongFormatVersion unknown = new UltraStarSongFormatVersion(EUltraStarSongFormatVersion.Unknown);
    public static readonly UltraStarSongFormatVersion v100 = new UltraStarSongFormatVersion(EUltraStarSongFormatVersion.V100);
    public static readonly UltraStarSongFormatVersion v110 = new UltraStarSongFormatVersion(EUltraStarSongFormatVersion.V110);
    public static readonly UltraStarSongFormatVersion v120 = new UltraStarSongFormatVersion(EUltraStarSongFormatVersion.V120);
    public static readonly UltraStarSongFormatVersion v200 = new UltraStarSongFormatVersion(EUltraStarSongFormatVersion.V200);

    public string StringValue { get; private set; }
    public EUltraStarSongFormatVersion EnumValue { get; private set; }

    public UltraStarSongFormatVersion(EUltraStarSongFormatVersion enumValue)
    {
        EnumValue = enumValue;
        StringValue = ToString(enumValue);
    }

    public UltraStarSongFormatVersion(string stringValue)
    {
        StringValue = NormalizeVersionString(stringValue);
        EnumValue = ToEnum(stringValue);
    }

    private static string ToString(EUltraStarSongFormatVersion versionEnum)
    {
        switch (versionEnum)
        {
            case EUltraStarSongFormatVersion.V100: return "1.0.0";
            case EUltraStarSongFormatVersion.V110: return "1.1.0";
            case EUltraStarSongFormatVersion.V120: return "1.2.0";
            case EUltraStarSongFormatVersion.V200: return "2.0.0";
            default: return EUltraStarSongFormatVersion.Unknown.ToString();
        }
    }

    private static EUltraStarSongFormatVersion ToEnum(string stringValue)
    {
        string normalizedVersionString = NormalizeVersionString(stringValue);
        return EnumUtils.GetValuesAsList<EUltraStarSongFormatVersion>()
            .FirstOrDefault(versionEnum => string.Equals(ToString(versionEnum), normalizedVersionString, StringComparison.InvariantCultureIgnoreCase));
    }

    private static string NormalizeVersionString(string versionString)
    {
        if (versionString.IsNullOrEmpty())
        {
            return v100.StringValue;
        }
        return versionString.TrimStart('v').TrimStart('V');
    }

    public bool IsBefore(UltraStarSongFormatVersion other)
    {
        if (EnumValue is EUltraStarSongFormatVersion.Unknown
            || other.EnumValue is EUltraStarSongFormatVersion.Unknown)
        {
            return false;
        }
        return (int)EnumValue < (int)other.EnumValue;
    }

    public bool IsAfter(UltraStarSongFormatVersion other)
    {
        if (EnumValue is EUltraStarSongFormatVersion.Unknown
            || other.EnumValue is EUltraStarSongFormatVersion.Unknown)
        {
            return false;
        }
        return (int)EnumValue > (int)other.EnumValue;
    }

    public override string ToString()
    {
        return $"{nameof(UltraStarSongFormatVersion)}({StringValue})";
    }
}
