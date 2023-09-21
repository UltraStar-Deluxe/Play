public static class BpmUtils
{

    public static double BeatToMillisecondsInSong(SongMeta songMeta, double beat)
    {
        return BeatToMillisecondsInSongWithoutGap(songMeta, beat) + songMeta.Gap;
    }

    public static double BeatToMillisecondsInSongWithoutGap(SongMeta songMeta, double beat)
    {
        double beatsPerMinute = GetBeatsPerMinute(songMeta);
        double millisecondsPerBeat = 60000.0 / beatsPerMinute;
        double millisecondsInSong = beat * millisecondsPerBeat;
        return millisecondsInSong;
    }

    public static double GetBeatsPerMinute(SongMeta songMeta)
    {
        return songMeta.BeatsPerMinute;
    }

    public static double GetBeatsPerSecond(SongMeta songMeta)
    {
        return GetBeatsPerMinute(songMeta) / 60.0;
    }

    public static double GetSamplesPerBeat(SongMeta songMeta, int sampleRate)
    {
        double secondsPerBeat = MillisecondsPerBeat(songMeta) / 1000.0;
        return secondsPerBeat * sampleRate;
    }

    public static double MillisecondInSongToBeat(SongMeta songMeta, double millisInSong)
    {
        return MillisecondInSongToBeatWithoutGap(songMeta, millisInSong - songMeta.Gap);
    }

    public static double MillisecondInSongToBeatWithoutGap(SongMeta songMeta, double millisInSong)
    {
        double beatsPerMinute = GetBeatsPerMinute(songMeta);
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
