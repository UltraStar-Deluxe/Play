// https://stackoverflow.com/questions/35062591/bytes-to-human-readable-string

using System;

public static class ByteSizeUtils
{
    private static readonly string[] sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    private const long ByteConversion = 1000;

    public static bool TryGetHumanReadableByteSize(long value, out double size, out string unit)
    {
        if (value == 0)
        {
            size = 0;
            unit = "B";
            return true;
        }
        
        if (value < 0)
        {
            TryGetHumanReadableByteSize(-value, out double negatedSize, out unit);
            size = -negatedSize;
            return true;
        }

        int mag = (int)Math.Log(value, ByteConversion);
        size = value / Math.Pow(1000, mag);
        unit = sizeSuffixes[mag];
        return true;
    }
}
