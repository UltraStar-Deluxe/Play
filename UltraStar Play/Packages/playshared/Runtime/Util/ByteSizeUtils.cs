// https://stackoverflow.com/questions/35062591/bytes-to-human-readable-string

using System;

public static class ByteSizeUtils
{
    private static readonly string[] sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    private const long ByteConversion = 1000;

    public static string GetHumanReadableByteSize(long value)
    {
        if (value < 0)
        {
            return "-" + GetHumanReadableByteSize(-value);
        }
        
        if (value == 0)
        {
            return "0 B";
        }

        int mag = (int)Math.Log(value, ByteConversion);
        double adjustedSize = value / Math.Pow(1000, mag);
        return $"{adjustedSize:n2} {sizeSuffixes[mag]}";
    }
}
