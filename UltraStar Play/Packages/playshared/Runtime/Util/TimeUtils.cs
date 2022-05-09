using System;

/**
 * Unity's Time.time can only be called from the main thread.
 * This class provides alternatives.
 */
public static class TimeUtils
{
    public static long GetUnixTimeMilliseconds()
    {
        // See https://stackoverflow.com/questions/4016483/get-time-in-milliseconds-using-c-sharp
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
}
