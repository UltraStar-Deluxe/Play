public static class SongMetaBpmUtils
{
    public static double BeatsPerSecond(SongMeta songMeta)
    {
        return BpmUtils.BeatsPerSecond(songMeta.BeatsPerMinute);
    }

    public static double BeatsToMillis(SongMeta songMeta, double beats)
    {
        return BeatsToMillisWithoutGap(songMeta, beats) + songMeta.GapInMillis;
    }

    public static double BeatsToMillisWithoutGap(SongMeta songMeta, double beats)
    {
        return BpmUtils.BeatsToMillisWithoutGap(songMeta.BeatsPerMinute, beats);
    }

    public static double MillisToBeats(SongMeta songMeta, double millisInSong)
    {
        return MillisToBeatsWithoutGap(songMeta, millisInSong - songMeta.GapInMillis);
    }

    public static double MillisToBeatsWithoutGap(SongMeta songMeta, double millisInSong)
    {
        return BpmUtils.MillisToBeatsWithoutGap(songMeta.BeatsPerMinute, millisInSong);
    }

    public static double MillisPerBeat(SongMeta songMeta)
    {
        double millisOfBeat0 = BeatsToMillisWithoutGap(songMeta, 0);
        double millisOfBeat1 = BeatsToMillisWithoutGap(songMeta, 1);
        return millisOfBeat1 - millisOfBeat0;
    }
}
