using System;

public static class BpmUtils
{
    public static double BeatsPerSecond(double beatsPerMinute)
    {
        return beatsPerMinute / 60.0;
    }

    public static double BeatsToMillisWithoutGap(double beatsPerMinute, double beats)
    {
        if (NumberUtils.IsDistanceLessThan(beatsPerMinute, 0, 1))
        {
            throw new ArgumentException($"{nameof(beatsPerMinute)} too small");
        }

        double millisecondsPerBeat = 60000.0 / beatsPerMinute;
        double millisecondsInSong = beats * millisecondsPerBeat;
        return millisecondsInSong;
    }

    public static double MillisToBeatsWithoutGap(double beatsPerMinute, double millisInSong)
    {
        double beatInSong = beatsPerMinute * millisInSong / 60000.0;
        return beatInSong;
    }
}
