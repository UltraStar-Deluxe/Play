public static class BpmUtils
{

    public static float BeatToMillisecondsInSong(SongMeta songMeta, float beat)
    {
        return BeatToMillisecondsInSongWithoutGap(songMeta, beat) + songMeta.Gap;
    }

    public static float BeatToMillisecondsInSongWithoutGap(SongMeta songMeta, float beat)
    {
        float beatsPerMinute = GetBeatsPerMinute(songMeta);
        float millisecondsPerBeat = 60000.0f / beatsPerMinute;
        float millisecondsInSong = beat * millisecondsPerBeat;
        return millisecondsInSong;
    }

    public static float GetBeatsPerMinute(SongMeta songMeta)
    {
        // UltraStar BPM is not "beats per minute" but "bars per minute" in four-four-time.
        // To get the common "beats per minute", one has to multiply with 4.
        return songMeta.Bpm * 4.0f;
    }

    public static float GetBeatsPerSecond(SongMeta songMeta)
    {
        return GetBeatsPerMinute(songMeta) / 60.0f;
    }

    public static float MillisecondInSongToBeat(SongMeta songMeta, float millisInSong)
    {
        return MillisecondInSongToBeatWithoutGap(songMeta, millisInSong - songMeta.Gap);
    }

    public static float MillisecondInSongToBeatWithoutGap(SongMeta songMeta, float millisInSong)
    {
        float beatsPerMinute = GetBeatsPerMinute(songMeta);
        float result = beatsPerMinute * millisInSong / 1000.0f / 60.0f;
        return result;
    }

    public static float MillisecondsPerBeat(SongMeta songMeta)
    {
        float millisOfBeat0 = BeatToMillisecondsInSong(songMeta, 0);
        float millisOfBeat1 = BeatToMillisecondsInSong(songMeta, 1);
        return millisOfBeat1 - millisOfBeat0;
    }
}
