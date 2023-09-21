public static class BpmUtils
{
    public static double BeatToMillisecondsInSong(SongMeta songMeta, double beat)
    {
        return BeatToMillisecondsInSongWithoutGap(songMeta, beat) + songMeta.GapInMillis;
    }

    public static double BeatToMillisecondsInSongWithoutGap(SongMeta songMeta, double beat)
    {
        return BeatToMillisecondsInSongWithoutGap(songMeta.BeatsPerMinute, beat);
    }

    public static double BeatToMillisecondsInSongWithoutGap(double beatsPerMinute, double beat)
    {
        double millisecondsPerBeat = 60000.0 / beatsPerMinute;
        double millisecondsInSong = beat * millisecondsPerBeat;
        return millisecondsInSong;
    }

    public static double GetBeatsPerSecond(SongMeta songMeta)
    {
        return GetBeatsPerSecond(songMeta.BeatsPerMinute);
    }

    public static double GetBeatsPerSecond(double beatsPerMinute)
    {
        return beatsPerMinute / 60.0;
    }

    public static double MillisecondInSongToBeat(SongMeta songMeta, double millisInSong)
    {
        return MillisecondInSongToBeatWithoutGap(songMeta, millisInSong - songMeta.GapInMillis);
    }

    public static double MillisecondInSongToBeatWithoutGap(SongMeta songMeta, double millisInSong)
    {
        return MillisecondInSongToBeatWithoutGap(songMeta.BeatsPerMinute, millisInSong);
    }

    public static double MillisecondInSongToBeatWithoutGap(double beatsPerMinute, double millisInSong)
    {
        double result = beatsPerMinute * millisInSong / 1000.0 / 60.0;
        return result;
    }

    public static double MillisecondsPerBeat(SongMeta songMeta)
    {
        double millisOfBeat0 = BeatToMillisecondsInSong(songMeta, 0);
        double millisOfBeat1 = BeatToMillisecondsInSong(songMeta, 1);
        return millisOfBeat1 - millisOfBeat0;
    }
}
