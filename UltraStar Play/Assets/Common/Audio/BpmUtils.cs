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
        // Ultrastar BPM is not "beats per minute" but "bars per minute" in four-four-time.
        // To get the common "beats per minute", one has to multiply with 4.
        return songMeta.Bpm * 4.0;
    }

    public static double GetBeatsPerSecond(SongMeta songMeta)
    {
        return GetBeatsPerMinute(songMeta) / 60.0;
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
